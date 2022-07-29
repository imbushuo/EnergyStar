namespace EnergyStar
{
    internal class Program
    {
        static void Main(string[] args)
        {
            HookManager.SubscribeToWindowEvents();
            EnergyManager.ThrottleAllUserProcessesOnStartup();

            while (true)
            {
                if (Event.PeekMessage(out Win32WindowForegroundMessage msg, IntPtr.Zero, 0, 0, Event.PM_REMOVE))
                {
                    if (msg.Message == Event.WM_QUIT) break;

                    Event.TranslateMessage(ref msg);
                    Event.DispatchMessage(ref msg);
                }
            }

            HookManager.UnsubscribeWindowEvents();
        }
    }
}