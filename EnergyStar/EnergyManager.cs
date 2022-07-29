using System.Runtime.InteropServices;
using System.Text;

namespace EnergyStar
{
    public unsafe class EnergyManager
    {
        public static readonly HashSet<string> BypassProcessList = new HashSet<string>
        {
            // Edge has energy awareness
            "msedge.exe",
            "WebViewHost.exe",
            // Fire extinguisher should not catch fire
            "taskmgr.exe",
            "procmon.exe",
            "procmon64.exe",
            // UWP - TODO
            "ApplicationFrameHost.exe",
            // Widgets
            "Widgets.exe",
            // System shell
            "explorer.exe",
            "ShellExperienceHost.exe",
            "StartExperienceHost.exe",
            "SearchHost.exe",
            "sihost.exe",
            // IME
            "ChsIME.exe",
            "ctfmon.exe",
#if DEBUG
            "devenv.exe",
#endif
        };

        private static uint pendingProcPid = 0;
        private static string pendingProcName = "";

        private static IntPtr pThrottleOn = IntPtr.Zero;
        private static IntPtr pThrottleOff = IntPtr.Zero;
        private static int szControlBlock = 0;

        static EnergyManager()
        {
            szControlBlock = Marshal.SizeOf<Win32Api.PROCESS_POWER_THROTTLING_STATE>();
            pThrottleOn = Marshal.AllocHGlobal(szControlBlock);
            pThrottleOff = Marshal.AllocHGlobal(szControlBlock);

            var throttleState = new Win32Api.PROCESS_POWER_THROTTLING_STATE
            {
                Version = Win32Api.PROCESS_POWER_THROTTLING_STATE.PROCESS_POWER_THROTTLING_CURRENT_VERSION,
                ControlMask = Win32Api.ProcessorPowerThrottlingFlags.PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
                StateMask = Win32Api.ProcessorPowerThrottlingFlags.PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
            };

            var unthrottleState = new Win32Api.PROCESS_POWER_THROTTLING_STATE
            {
                Version = Win32Api.PROCESS_POWER_THROTTLING_STATE.PROCESS_POWER_THROTTLING_CURRENT_VERSION,
                ControlMask = Win32Api.ProcessorPowerThrottlingFlags.PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
                StateMask = Win32Api.ProcessorPowerThrottlingFlags.None,
            };

            Marshal.StructureToPtr(throttleState, pThrottleOn, false);
            Marshal.StructureToPtr(unthrottleState, pThrottleOff, false);
        }

        private static void ToggleEcoQoS(IntPtr hProcess, bool enable)
        {
            if (!Win32Api.SetProcessInformation(hProcess, Win32Api.PROCESS_INFORMATION_CLASS.ProcessPowerThrottling,
                enable ? pThrottleOn : pThrottleOff, (uint) szControlBlock))
            {
                Console.WriteLine($"Unable to set EcoQos {enable} for previous app: {Marshal.GetLastWin32Error()}");
            }
        }

        public static unsafe void HandleForegroundEvent(IntPtr hwnd)
        {
            var windowThreadId = Win32Api.GetWindowThreadProcessId(hwnd, out uint procId);
            // This is invalid, likely a process is dead, or idk
            if (windowThreadId == 0 || procId == 0) return;

            var procHandle = Win32Api.OpenProcess(
                (uint) (Win32Api.ProcessAccessFlags.QueryLimitedInformation | Win32Api.ProcessAccessFlags.SetInformation), false, procId);
            if (procHandle == IntPtr.Zero) return;

            int capacity = 1024;
            var sb = new StringBuilder(capacity);

            if (Win32Api.QueryFullProcessImageName(procHandle, 0, sb, ref capacity))
            {
                // Boost the current foreground app, and then impose EcoQoS for previous foreground app
                var appName = Path.GetFileName(sb.ToString());
                var bypass = BypassProcessList.Contains(appName);

                if (!bypass)
                {
                    Console.WriteLine($"Boost {appName}");
                    ToggleEcoQoS(procHandle, false);
                }
               
                if (pendingProcPid != 0)
                {
                    Console.WriteLine($"Throttle {pendingProcName}");

                    var prevProcHandle = Win32Api.OpenProcess((uint)Win32Api.ProcessAccessFlags.SetInformation, false, pendingProcPid);
                    if (prevProcHandle != IntPtr.Zero)
                    {
                        ToggleEcoQoS(prevProcHandle, true);
                        Win32Api.CloseHandle(prevProcHandle);
                    }
                    else
                    {
                        Console.WriteLine("W: Unable to open handle for previous foreground app");
                    }
                }

                if (!bypass)
                {
                    pendingProcPid = procId;
                    pendingProcName = appName;
                }
            }

            Win32Api.CloseHandle(procHandle);
        }
    }
}
