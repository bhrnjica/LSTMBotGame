using CNTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;

namespace MarioKart.Bot.NET
{
    class MarioKartPlay
    {
        static Random m_rand = new Random((int)DateTime.Now.Ticks);
        //model name (should exist before playing game)
        public static string cntkModel = "../../../../training/mario_kart_modelv1";

        static Function m_model=null;

        public static void LoadModel(string modelFileName, DeviceDescriptor device)
        {
           
            //load the model from disk
            m_model = Function.Load(modelFileName, device);

            return;
        }
        static InputSimulator m_InputSimulator = new InputSimulator();


        public static bool activateN64Emulator()
        {
            // retrieve Notepad main window handle
            IntPtr hWnd = InterceptKeys.FindWindow("Project64 2.3.2.202", null);
            InputSimulator sim = new InputSimulator();
            if (!hWnd.Equals(IntPtr.Zero) && hWnd == InterceptKeys.GetForegroundWindow())
            {
                
                //we dont want to block user to make focus on another window
                //InterceptKeys.SetForegroundWindow(hWnd);

                //InterceptKeys.SetForegroundWindow(hWnd); //Activate Handle By Process
               // InterceptKeys.ShowWindow(hWnd, InterceptKeys.SW_RESTORE); //Maximizes Window in case it was minimized.
                //sim.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.RETURN);
                return true;
            }
            else
                return false;
        }

        private static void ReleaseKey(string v)
        {
            var vk = getVK(v);
            m_InputSimulator.Keyboard.KeyUp(vk);
            //System.Threading.Thread.Sleep(900);
        }

        private static VirtualKeyCode getVK(string v)
        {
            if (v == "W")
                return WindowsInput.Native.VirtualKeyCode.VK_W;
            if (v == "A")
                return WindowsInput.Native.VirtualKeyCode.VK_A;
            if (v == "F")
                return WindowsInput.Native.VirtualKeyCode.VK_F;
            if (v == "S")
                return WindowsInput.Native.VirtualKeyCode.VK_S;
            else
                return WindowsInput.Native.VirtualKeyCode.SPACE;
        }

        private static void PressKey(string v)
        {
            var vk = getVK(v);
            m_InputSimulator.Keyboard.KeyDown(vk);
        }
        public static void MoveForward()
        {
            ReleaseKey("A");
            ReleaseKey("F");
            ReleaseKey("S");
            //ReleaseKey("W");
            PressKey("W");

            
            Console.WriteLine("---Move Forward--");
        }
        public static void TurnLeft()
        {
            //ReleaseKey("A");
            PressKey("A");
            ReleaseKey("W");
            ReleaseKey("S");
            ReleaseKey("F");
            Console.WriteLine("---Move Left--");
        }
        public static void TurnRight()
        {
           // ReleaseKey("F");
            PressKey("F");
            ReleaseKey("A");
            ReleaseKey("S");
            Console.WriteLine("---Move Right--");
        }
        public static void Break()
        {
            ReleaseKey("A");
            ReleaseKey("W");
            ReleaseKey("F");
            //ReleaseKey("S");
            PressKey("S");
            

            Console.WriteLine("---Break-Reverse--");
        }
        public static void MoveForwardLeft()
        {
            ReleaseKey("F");
            ReleaseKey("S");
            //ReleaseKey("W");
            //ReleaseKey("A");
            PressKey("W");
            PressKey("A");
           
            Console.WriteLine("---Move Forward Left--");
        }
        public static void MoveForwardRight()
        {
            ReleaseKey("A");
            ReleaseKey("S");
            //ReleaseKey("W");
            //ReleaseKey("F");
            PressKey("W");
            PressKey("F");
           

            Console.WriteLine("---Move Forward Right--");
        }
        public static void BreakLeft()
        {
            ReleaseKey("S");
            ReleaseKey("A");
            PressKey("S");
            PressKey("A");
            ReleaseKey("W");
            ReleaseKey("F");

            Console.WriteLine("---Break Left--");
        }

        public static void BreakRight()
        {
            ReleaseKey("F");
            ReleaseKey("S");
            PressKey("F");
            PressKey("S");
            ReleaseKey("W");
            ReleaseKey("A");
            Console.WriteLine("---Break Right--");
        }
        
        public static void None()
        {
            if (m_rand.Next(0, 3) == 1)
                PressKey("W");
            else
                ReleaseKey("W");

            ReleaseKey("A");
            ReleaseKey("S");
            ReleaseKey("F");

            Console.WriteLine("---None--");
        }
        
       


        public static void PlayGame(DeviceDescriptor device)
        {
            ////test for image show
            //var wnd = new ImgWnd();
            ////show image dialog from different thread
            //Task mytask = Task.Run(() => { wnd.ShowDialog(); });

            Task.Delay(2000).Wait();
            //wait for quit key
            while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape))
            {
                //capture image at specific location and size
                var rec = new Rectangle() { X = GenerateData.m_ImageX, Y = GenerateData.m_ImageY, Width = GenerateData.m_ImageWidth, Height = GenerateData.m_ImageHeight };
                var b = GenerateData.Capture(rec);
                //process Image
                //var b1 = GenerateData.ResizeAndGray(b, true);
                //wnd.LoadImage(b1);
                var retValue = GenerateData.ResizeAndCHWExctraction(b, true).ToArray();
                Play(retValue, device);
            }
        }

        static void Play(float[] xVal, DeviceDescriptor device)
        {
            //extract features and label from the model
            Variable feature = m_model.Arguments[0];
            Variable label = m_model.Outputs.Last();

            Value xValues = Value.CreateBatch<float>(feature.Shape, xVal, device);

            //map the variables and values
            var inputDataMap = new Dictionary<Variable, Value>();
            inputDataMap.Add(feature, xValues);
            var outputDataMap = new Dictionary<Variable, Value>();
            outputDataMap.Add(label, null);

            //evaluate the model
            m_model.Evaluate(inputDataMap, outputDataMap, device);
            //extract the result  as one hot vector
            var outputData = outputDataMap[label].GetDenseData<float>(label);
            bool skip = true;
            foreach(var val in outputData.First())
            {
                Console.Write($"{val},");
                if (!float.IsNaN(val) && val!=float.MinValue && val != float.MaxValue)
                    skip = false;

            }
            Console.WriteLine($" ");
            if (skip)
                return;

            var outValue = outputData.Select((IList<float> l) => l.IndexOf(l.Max())).FirstOrDefault();

            if (!activateN64Emulator())
                return;
            //
            if (outValue == 0)
                MoveForward();
            else if (outValue == 1)
                Break();
            else if (outValue == 2)
                MoveForwardLeft();
            else if (outValue == 3)
                MoveForwardRight();
 
            else
                None();

        }

    }
}
