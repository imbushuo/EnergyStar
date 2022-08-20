using EnergyStar.Interop;
using Microsoft.Win32;

namespace EnergyStar
{
    internal class Program
    {
        static CancellationTokenSource cts = new CancellationTokenSource();

        static async void HouseKeepingThreadProc()
        {
            Console.WriteLine("House keeping thread started.");
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    var houseKeepingTimer = new PeriodicTimer(TimeSpan.FromMinutes(5));
                    await houseKeepingTimer.WaitForNextTickAsync(cts.Token);
                    EnergyManager.ThrottleAllUserBackgroundProcesses();
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        static void SystemEventsPowerModeChanged()
        {
            Win32Api.SYSTEM_POWER_STATUS powerStatus = Win32Api.GetSystemPowerStatus();

            switch (powerStatus.ACLineStatus)
            {
                case Win32Api.SYSTEM_POWER_STATUS.AC_LINE_STATUS_OFFLINE:
                    EnergyManager.IsAcConnected = false;
                    break;
                case Win32Api.SYSTEM_POWER_STATUS.AC_LINE_STATUS_ONLINE:
                    EnergyManager.IsAcConnected = true;
                    break;
                default:
                    break;
            }

            EnergyManager.ThrottleAllUserBackgroundProcesses();
        }

        static void Main(string[] args)
        {
            // Well, this program only works for Windows Version starting with Cobalt...
            // Nickel or higher will be better, but at least it works in Cobalt
            //
            // In .NET 5.0 and later, System.Environment.OSVersion always returns the actual OS version.
            if (Environment.OSVersion.Version.Build < 22000)
            {
                Console.WriteLine("E: You are too poor to use this program.");
                Console.WriteLine("E: Please upgrade to Windows 11 22H2 for best result, and consider ThinkPad Z13 as your next laptop.");
                // ERROR_CALL_NOT_IMPLEMENTED
                Environment.Exit(120);
            }

            // Call SystemEventsPowerModeChanged() to init the power adapter status.
            SystemEventsPowerModeChanged();

            HookManager.SubscribeToWindowEvents();
            EnergyManager.ThrottleAllUserBackgroundProcesses();

            var houseKeepingThread = new Thread(new ThreadStart(HouseKeepingThreadProc));
            houseKeepingThread.Start();

#pragma warning disable CA1416 // Validate platform compatibility
            SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler((object sender, PowerModeChangedEventArgs e) => SystemEventsPowerModeChanged());
#pragma warning restore CA1416 // Validate platform compatibility

            while (true)
            {
                if (Event.GetMessage(out Win32WindowForegroundMessage msg, IntPtr.Zero, 0, 0))
                {
                    if (msg.Message == Event.WM_QUIT)
                    {
                        cts.Cancel();
                        break;
                    }

                    Event.TranslateMessage(ref msg);
                    Event.DispatchMessage(ref msg);
                }
            }

            cts.Cancel();
            HookManager.UnsubscribeWindowEvents();
        }
    }
}
