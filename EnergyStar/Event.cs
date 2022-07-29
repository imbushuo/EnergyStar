using System.Runtime.InteropServices;

namespace EnergyStar
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Win32WindowForegroundMessage
    {
        public IntPtr Hwnd;
        public uint Message;
        public IntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public System.Drawing.Point Point;
    }

    internal class Event
    {
        public const uint PM_NOREMOVE = 0;
        public const uint PM_REMOVE = 1;

        public const uint WM_QUIT = 0x0012;

        [DllImport("user32.dll")]
        public static extern bool PeekMessage(out Win32WindowForegroundMessage lpMsg, IntPtr hwnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        [DllImport("user32.dll")]
        public static extern bool GetMessage(out Win32WindowForegroundMessage lpMsg, IntPtr hwnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        public static extern bool TranslateMessage(ref Win32WindowForegroundMessage lpMsg);

        [DllImport("user32.dll")]
        public static extern IntPtr DispatchMessage(ref Win32WindowForegroundMessage lpMsg);
    }
}
