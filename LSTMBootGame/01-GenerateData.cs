using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Globalization;
using System.IO;

namespace MarioKart.Bot.NET
{
    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    /// <summary>
    /// Class implements data generation from playing MArioKArt. Emulator must be positioned on top-left screen corner,
    /// with Windowed 800x600 resolution
    /// </summary>
    internal class GenerateData
    {

        public static LowLevelKeyboardProc _proc = HookKeyCallback;
        public static IntPtr _hookID = IntPtr.Zero;
        //phase three training
        public static int m_FileCounter = 1;
        public static string trainingFilePath = "../../../../training/training_data_{0}.txt";
        
        //position and rectangle size
        public static int m_ImageX = 150;
        public static int m_ImageY = 180;
        public static int m_ImageWidth = 490;
        public static int m_ImageHeight = 365;
        public static int m_ResizeWidth = 100;
        public static int m_ResizeHeight = 74;
        public static int m_ResizeDepth = 1;//grayscale 1; RGB = 3

        public static ImgWnd m_wndImage;
        public static bool m_WPressed, m_SPressed;
        public static string m_OutputLabel = "";
        static public async Task<int> Start()
        {
            Console.WriteLine("You have 5 second to prepare the game for training!");
            Task.Delay(5000).Wait();
            Console.WriteLine("Training has been started!");
            //capture screen and keyinput
            Task.Run(() => CaptureGameControlKeys());
            CaptureImageFromGame(true, false, false);

            return 0;
        }

        /// <summary>
        /// Intercepts the Keys in put and process them
        /// </summary>
        private static void CaptureGameControlKeys()
        {
            _hookID = InterceptKeys.SetHook(_proc);
            Application.Run();
            InterceptKeys.UnhookWindowsHookEx(_hookID);
        }

        /// <summary>
        /// Captures the image from screen and show it on another wnd
        /// </summary>
        /// <param name="showImage"></param>
        /// <param name="showProgress"></param>
        private static void CaptureImageFromGame(bool procesImage= true, bool showImage = false, bool showProgress = false)
        {
            Stopwatch sw = null;
            //Create img wnd   
            if (showImage)
            {
                m_wndImage = new ImgWnd();
                //show image dialog from different thread in order the current thread be available for receiving input
                Task mytask = Task.Run(() => { m_wndImage.ShowDialog(); });

                m_wndImage.Size = new Size(m_ImageWidth, m_ImageHeight + 30);
            }

            if (showProgress)
                sw = Stopwatch.StartNew();

            //wait for quit key
            while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape))
            {
                //reset stop watch
                if (showProgress)
                    sw.Restart();

                //capture image at specific location and size
                var rec = new Rectangle() { X = m_ImageX, Y = m_ImageY, Width = m_ImageWidth, Height = m_ImageHeight };
                var b = Capture(rec);

                
                //show image
                if (showImage)
                {
                    //resize and gray scale 
                    //var img = b.Resize(GenerateData.m_ResizeWidth, GenerateData.m_ResizeHeight, true);
                    var img = ImageUtil.MakeGrayscale(b);

                    m_wndImage.LoadImage(img);
                }
               
                // 
                if (showProgress)
                    Console.WriteLine($"Frame took {sw.Elapsed.Milliseconds} milliseconds.");
                //process Image
                if(procesImage)
                    createTrainingData(b);
            }
        }

        static DateTime time = DateTime.Now;
        private static void createTrainingData(Bitmap img)
        {
            if (string.IsNullOrEmpty(m_OutputLabel))
            {
                if (m_WPressed)
                    m_OutputLabel = InputKeyToHotVector(Keys.W);
                else
                    return;
            }
               

            //we want only 3 image per second 
            if ((DateTime.Now - time).Milliseconds < 100)
                return;
            //reset tim3
            time = DateTime.Now;


            //
            Console.WriteLine("Image is processed and stored in the file!");
            var strBuilder = processImageForTraining(img, m_ResizeDepth == 1 /* for grayscale */);

            
            //once the output is retrieved reset it in order to get better and most frequent images
            var outLabel = "|label " + m_OutputLabel;
            strBuilder.Insert(0, outLabel);
            Console.WriteLine(m_OutputLabel);
            //reset output
            m_OutputLabel = "";

            //format training file path
            var strPath = string.Format(trainingFilePath, m_FileCounter);

            //create if doesn't exist
            if (!File.Exists(strPath))
            {
                using (var sw = File.Create(strPath))
                {

                }
            }

            //append line
            using (StreamWriter sw = File.AppendText(strPath))
            {
                var strLine = strBuilder.ToString();
                sw.WriteLine(strLine);
            }

        }

        /// <summary>
        /// Convert image into feature row
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public  static StringBuilder processImageForTraining(Bitmap bitmap, bool grayScaleImage=true)
        {
            List<float> retValue = ResizeAndCHWExctraction(bitmap, grayScaleImage);
            Console.WriteLine($"Number of features ={retValue.Count}");
            StringBuilder sb = new StringBuilder();
            sb.Append("\t|features ");

            for (int i = 0; i < retValue.Count; i++)
            {
                var strVal = retValue[i].ToString(CultureInfo.InvariantCulture);
                if (retValue.Count > i + 1)
                    strVal += " ";

                sb.Append(strVal);
            }
            return sb;
        }
        /// <summary>
        /// Takes the original captured image resizet it and transform into floating numbers
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static List<float> ResizeAndCHWExctraction(Bitmap bitmap, bool makeGrayScale)
        {
            var imageToProcess = ResizeAndGray(bitmap, makeGrayScale);

            var retValue = ImageUtil.ParallelExtractCHW(imageToProcess, makeGrayScale);
            return retValue;
        }

        /// <summary>
        /// Resize bitmap to size for training
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="makeGrayScale"></param>
        /// <returns></returns>
        public static Bitmap ResizeAndGray(Bitmap bitmap, bool makeGrayScale)
        {
            Bitmap bmp = null;

            var img = ImageUtil.Resize(bitmap, m_ResizeWidth, m_ResizeHeight, true);

            if (makeGrayScale)
                bmp = ImageUtil.MakeGrayscale(img);

            var imageToProcess = makeGrayScale ? bmp : img;

            return imageToProcess;
        }

        /// <summary>
        /// Captures screen region 
        /// </summary>
        /// <param name="Region">captured image</param>
        /// <returns></returns>
        public static Bitmap Capture(Rectangle Region)
        {
            var bmp = new Bitmap(Region.Width, Region.Height);

            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(Region.Location, Point.Empty, Region.Size, CopyPixelOperation.SourceCopy);

                g.Flush(System.Drawing.Drawing2D.FlushIntention.Sync);
            }

            return bmp;
        }

        /// <summary>
        /// Callback method to report the intercepted input key 
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private static IntPtr HookKeyCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            //when key is pressed
            if (nCode >= 0 && wParam == (IntPtr)InterceptKeys.WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                //convert key to One-Hot Vector
                m_OutputLabel = InputKeyToHotVector((Keys)vkCode);

                //
                if ((Keys)vkCode == Keys.W)
                    m_WPressed = true;
                if ((Keys)vkCode == Keys.S)
                    m_SPressed = true;
            }
            //control  when W and S is released
            if (nCode >= 0 && wParam == (IntPtr)InterceptKeys.WM_KEYUP)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if ((Keys)vkCode == Keys.W)
                    m_WPressed = false;
                if ((Keys)vkCode == Keys.S)
                    m_SPressed = false;
            }

            return InterceptKeys.CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
        /// <summary>
        ///Convert keys to a...multi-hot...array adn CNTK format
        /// |labels 0 1 0 0 0 0 	
        /// </summary>
        /// <param name="key"></param>
        public static string InputKeyToHotVector(Keys key)
        {
            var output = new string[9];
            if (Keys.W == key)
                return "1 0 0 0 0 ";//Keys W is pressed
            else if (Keys.S == key)
                return "0 1 0 0 0 ";//Keys S is pressed
            else if (m_WPressed && Keys.A == key)
                return "0 0 1 0 0 ";//Keys A and W are pressed
            else if (m_WPressed && Keys.F == key)
                return "0 0 0 1 0 ";//Keys F and W are pressed
            else
                return "0 0 0 0 1 ";//All keys are unpressed

        }
        /// <summary>
        ///Convert keys to a...multi-hot...array adn CNTK format
        /// |labels 0 1 0 0 0 0 0 0 0 0	
        /// </summary>
        /// <param name="key"></param>
        public static string InputKeyToHotVector1(Keys key)
        {
            var output = new string[9];
            if (Keys.None == key)
                return "|label 0 0 0 0 0 0 0 0 1 ";
            else if (Keys.W == key)
                return "|label 1 0 0 0 0 0 0 0 0 ";
            else if (Keys.S == key)
                return "|label 0 1 0 0 0 0 0 0 0 ";
            else if (m_WPressed && Keys.A == key)
                return "|label 0 0 0 0 1 0 0 0 0 ";
            else if (m_WPressed && Keys.F == key)
                return "|label 0 0 0 0 0 1 0 0 0 ";
            else if (m_SPressed && Keys.A == key)
                return "|label 0 0 0 0 0 0 1 0 0 ";
            else if (m_SPressed && Keys.F == key)
                return "|label 0 0 0 0 0 0 0 1 0 ";
            else if (Keys.A == key)
                return "|label 0 0 1 0 0 0 0 0 0 ";
            else if (Keys.F == key)
                return "|label 0 0 0 1 0 0 0 0 0 ";
            else

                return "|label 0 0 0 0 0 0 0 0 1 ";

        }

    }
}
