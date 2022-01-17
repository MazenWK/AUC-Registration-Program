using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Registration_Program
{
    public static class ConsoleWindow
    {
        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(HandleRef hWnd, int nCmdShow);
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindowByCaption(IntPtr zeroOnly, string lpWindowName);

        private const int SwRestore = 9;
        
        public static void Focus()
        {
            string originalTitle = Console.Title;
            string uniqueTitle = Guid.NewGuid().ToString();
            Console.Title = uniqueTitle;
            Thread.Sleep(50);
            IntPtr handle = FindWindowByCaption(IntPtr.Zero, uniqueTitle);

            Console.Title = originalTitle;

            ShowWindowAsync(new HandleRef(null, handle), SwRestore);
            SetForegroundWindow(handle);
        }
    }
}