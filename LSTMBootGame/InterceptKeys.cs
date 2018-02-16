using System;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;
using WindowsInput;

namespace MarioKart.Bot.NET
{
    //blogs.msdn.com/b/toub/archive/2006/05/03/589423.aspx
    public class InterceptKeys
    {

        public const int WH_KEYBOARD_LL = 13;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP   = 0x0101;
        public const int WM_SETTEXT = 0x000C;
        public const int SW_SHOWNORMAL = 1;
        public const int SW_SHOWMAXIMIZED = 3;
        public const int SW_RESTORE = 9;

        //private static LowLevelKeyboardProc _proc = HookCallback;
        //private static IntPtr _hookID = IntPtr.Zero;

        //public static void Main()
        //{
        //    _hookID = SetHook(_proc);
        //    Application.Run();
        //    UnhookWindowsHookEx(_hookID);
        //}





        //private static IntPtr HookCallback(
        //    int nCode, IntPtr wParam, IntPtr lParam)
        //{
        //    if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        //    {
        //        int vkCode = Marshal.ReadInt32(lParam);
        //        Console.WriteLine((Keys)vkCode);
        //    }

        //    return CallNextHookEx(_hookID, nCode, wParam, lParam);
        //}

        public static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {

                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]

        public static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        // import the function in your class
        [DllImport("User32.dll")]
        public static extern int SetForegroundWindow(IntPtr point);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(
            string lpClassName,
            string lpWindowName);

        [DllImport("User32.dll")]
        private static extern IntPtr FindWindowEx(
            IntPtr hwndParent,
            IntPtr hwndChildAfter,
            string lpszClass,
        string lpszWindows);
        [DllImport("User32.dll")]
        private static extern Int32 SendMessage(
            IntPtr hWnd,
            int Msg,
            IntPtr wParam,
        StringBuilder lParam);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);


        public static void SendKeys()
        {
            System.Threading.Thread.Sleep(2000);
            // retrieve Notepad main window handle
            IntPtr hWnd = FindWindow("Project64 2.3.2.202", null);
            InputSimulator sim = new InputSimulator();
            if (!hWnd.Equals(IntPtr.Zero))
            {
                SetForegroundWindow(hWnd);

                SetForegroundWindow(hWnd); //Activate Handle By Process
                ShowWindow(hWnd, SW_RESTORE); //Maximizes Window in case it was minimized.
                sim.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.RETURN);
                

                while (true)
                {
                    // send WM_SETTEXT message with "Hello World!"
                    sim.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.VK_W);
                    System.Threading.Thread.Sleep(900);
                    sim.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.VK_W);
                    System.Threading.Thread.Sleep(500);
                    //sim.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.VK_W);
                    //SendMessage(hWnd, WM_KEYDOWN, IntPtr.Zero, new StringBuilder("W"));
                    //Console.WriteLine("Key sent W!");
                    //                    System.Threading.Thread.Sleep(150);

                }


                //// send "Hello World!"
                //SendKeys.Send("Hello World!");
                //// send key "Tab"
                //SendKeys.Send("{TAB}");
                //// send key "Enter"
                //SendKeys.Send("{ENTER}");
            }
        }
    

    }
}

