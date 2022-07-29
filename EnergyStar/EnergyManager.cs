using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace EnergyStar
{
    public unsafe class EnergyManager
    {
        public static readonly HashSet<string> BypassProcessList = new HashSet<string>
        {
            // Not ourselves,
            "EnergyStar.exe",
            // Edge has energy awareness
            "msedge.exe",
            "WebViewHost.exe",
            // UWP Frame has special handling, should not be throttled,
            "ApplicationFrameHost.exe",
            // Fire extinguisher should not catch fire
            "taskmgr.exe",
            "procmon.exe",
            "procmon64.exe",
            // Widgets
            "Widgets.exe",
            // System shell
            "explorer.exe",
            "ShellExperienceHost.exe",
            "StartMenuExperienceHost.exe",
            "SearchHost.exe",
            "sihost.exe",
            // IME
            "ChsIME.exe",
            "ctfmon.exe",
#if DEBUG
            // Visual Studio
            "devenv.exe",
#endif
            // System Service - they have their awareness
            "svchost.exe",
            // WUDF
            "WUDFRd.exe",
        };
        public const string UWPFrameHostApp = "ApplicationFrameHost.exe";

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

        private static void ToggleEfficiencyMode(IntPtr hProcess, bool enable)
        {
            if (!Win32Api.SetProcessInformation(hProcess, Win32Api.PROCESS_INFORMATION_CLASS.ProcessPowerThrottling,
                enable ? pThrottleOn : pThrottleOff, (uint) szControlBlock))
            {
                Console.WriteLine($"Unable to set EcoQos {enable} for app: {hProcess} {Marshal.GetLastWin32Error()}");
            }
            if (!Win32Api.SetPriorityClass(hProcess, enable ? Win32Api.PriorityClass.IDLE_PRIORITY_CLASS : Win32Api.PriorityClass.NORMAL_PRIORITY_CLASS))
            {
                Console.WriteLine($"Unable to set priority {enable} for app: {hProcess} {Marshal.GetLastWin32Error()}");
            }
        }

        private static string GetProcessNameFromHandle(IntPtr hProcess)
        {
            int capacity = 1024;
            var sb = new StringBuilder(capacity);

            if (Win32Api.QueryFullProcessImageName(hProcess, 0, sb, ref capacity))
            {
                return Path.GetFileName(sb.ToString());
            }

            return "";
        }

        public static unsafe void HandleForegroundEvent(IntPtr hwnd)
        {
            var windowThreadId = Win32Api.GetWindowThreadProcessId(hwnd, out uint procId);
            // This is invalid, likely a process is dead, or idk
            if (windowThreadId == 0 || procId == 0) return;

            var procHandle = Win32Api.OpenProcess(
                (uint) (Win32Api.ProcessAccessFlags.QueryLimitedInformation | Win32Api.ProcessAccessFlags.SetInformation), false, procId);
            if (procHandle == IntPtr.Zero) return;

            // Get the process
            var appName = GetProcessNameFromHandle(procHandle);
            
            // UWP needs to be handled in a special case
            if (appName == UWPFrameHostApp)
            {
                var found = false;
                Win32Api.EnumChildWindows(hwnd, (innerHwnd, lparam) =>
                {
                    if (found) return true;
                    if (Win32Api.GetWindowThreadProcessId(innerHwnd, out uint innerProcId) > 0)
                    {
                        if (procId == innerProcId) return true;

                        var innerProcHandle = Win32Api.OpenProcess((uint)(Win32Api.ProcessAccessFlags.QueryLimitedInformation |
                            Win32Api.ProcessAccessFlags.SetInformation), false, innerProcId);
                        if (innerProcHandle == IntPtr.Zero) return true;

                        // Found. Set flag, reinitialize handles and call it a day
                        found = true;
                        Win32Api.CloseHandle(procHandle);
                        procHandle = innerProcHandle;
                        procId = innerProcId;
                        appName = GetProcessNameFromHandle(procHandle);
                    }

                    return true;
                }, IntPtr.Zero);
            }

            // Boost the current foreground app, and then impose EcoQoS for previous foreground app
            var bypass = BypassProcessList.Contains(appName);
            if (!bypass)
            {
                Console.WriteLine($"Boost {appName}");
                ToggleEfficiencyMode(procHandle, false);
            }

            if (pendingProcPid != 0)
            {
                Console.WriteLine($"Throttle {pendingProcName}");

                var prevProcHandle = Win32Api.OpenProcess((uint)Win32Api.ProcessAccessFlags.SetInformation, false, pendingProcPid);
                if (prevProcHandle != IntPtr.Zero)
                {
                    ToggleEfficiencyMode(prevProcHandle, true);
                    Win32Api.CloseHandle(prevProcHandle);
                    pendingProcPid = 0;
                    pendingProcName = "";
                }
            }

            if (!bypass)
            {
                pendingProcPid = procId;
                pendingProcName = appName;
            }

            Win32Api.CloseHandle(procHandle);
        }
    }
}
