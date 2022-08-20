using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace EnergyStar.Interop
{
    internal class Win32Api
    {
        public static Win32Api.SYSTEM_POWER_STATUS GetSystemPowerStatus()
        {
            IntPtr powerStatusPtr = Marshal.AllocHGlobal(Marshal.SizeOf<Win32Api.SYSTEM_POWER_STATUS>());

            try
            {
                if (Win32Api.GetSystemPowerStatus(powerStatusPtr))
                {
                    return Marshal.PtrToStructure<Win32Api.SYSTEM_POWER_STATUS>(powerStatusPtr);
                }

                return new Win32Api.SYSTEM_POWER_STATUS();
            }
            finally
            {
                Marshal.FreeHGlobal(powerStatusPtr);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] int dwFlags, [Out] StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetProcessInformation([In] IntPtr hProcess,
            [In] PROCESS_INFORMATION_CLASS ProcessInformationClass, IntPtr ProcessInformation, uint ProcessInformationSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetPriorityClass(IntPtr handle, PriorityClass priorityClass);

        [DllImport("kernel32.dll", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        public delegate bool WindowEnumProc(IntPtr hwnd, IntPtr lparam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr hwnd, WindowEnumProc callback, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetSystemPowerStatus(IntPtr lpSystemPowerStatus);

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        public enum PROCESS_INFORMATION_CLASS
        {
            ProcessMemoryPriority,
            ProcessMemoryExhaustionInfo,
            ProcessAppMemoryInfo,
            ProcessInPrivateInfo,
            ProcessPowerThrottling,
            ProcessReservedValue1,
            ProcessTelemetryCoverageInfo,
            ProcessProtectionLevelInfo,
            ProcessLeapSecondInfo,
            ProcessInformationClassMax,
        }

        [Flags]
        public enum ProcessorPowerThrottlingFlags : uint
        {
            None = 0x0,
            PROCESS_POWER_THROTTLING_EXECUTION_SPEED = 0x1,
        }

        public enum PriorityClass : uint
        {
            ABOVE_NORMAL_PRIORITY_CLASS = 0x8000,
            BELOW_NORMAL_PRIORITY_CLASS = 0x4000,
            HIGH_PRIORITY_CLASS = 0x80,
            IDLE_PRIORITY_CLASS = 0x40,
            NORMAL_PRIORITY_CLASS = 0x20,
            PROCESS_MODE_BACKGROUND_BEGIN = 0x100000,// 'Windows Vista/2008 and higher
            PROCESS_MODE_BACKGROUND_END = 0x200000,//   'Windows Vista/2008 and higher
            REALTIME_PRIORITY_CLASS = 0x100
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_POWER_THROTTLING_STATE
        {
            public const uint PROCESS_POWER_THROTTLING_CURRENT_VERSION = 1;

            public uint Version;
            public ProcessorPowerThrottlingFlags ControlMask;
            public ProcessorPowerThrottlingFlags StateMask;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_POWER_STATUS
        {
            public const Byte AC_LINE_STATUS_OFFLINE = 0;           // AC adapter disconnected
            public const Byte AC_LINE_STATUS_ONLINE = 1;            // AC adapter connected
            public const Byte AC_LINE_STATUS_UNKNOWN = 255;

            public const Byte BATTERY_FLAG_HIGH = 1;                // the battery capacity is at more than 66 percent
            public const Byte BATTERY_FLAG_LOW = 2;                 // the battery capacity is at less than 33 percent
            public const Byte BATTERY_FLAG_CRITICAL = 4;            // the battery capacity is at less than five percent
            public const Byte BATTERY_FLAG_CHARGING = 8;            // Charging
            public const Byte BATTERY_FLAG_NO_SYSTEM_BATTERY = 128; // No system battery
            public const Byte BATTERY_FLAG_UNKNOWN = 255;           // Unable to read the battery flag information

            public const Byte BATTERY_LIFE_PERCENT_UNKNOWN = 255;

            public const Byte SYSTEM_STATUS_FLAG_BATTERY_SAVER_OFF = 0; // Battery saver is off.
            public const Byte SYSTEM_STATUS_FLAG_BATTERY_SAVER_ON = 1;  // Battery saver on. Save energy where possible.

            public Byte ACLineStatus;           // The AC power status.
            public Byte BatteryFlag;            // The battery charge status.
            public Byte BatteryLifePercent;     // The percentage of full battery charge remaining. This member can be a value in the range 0 to 100, or 255 if status is unknown.
            public Byte SystemStatusFlag;       // The status of battery saver.
            public UInt32 BatteryLifeTime;      // The number of seconds of battery life remaining, or –1 if remaining seconds are unknown or if the device is connected to AC power.
            public UInt32 BatteryFullLifeTime;  // The number of seconds of battery life when at full charge, or –1 if full battery lifetime is unknown or if the device is connected to AC power.

            public SYSTEM_POWER_STATUS()
            {
                ACLineStatus = AC_LINE_STATUS_UNKNOWN;
                BatteryFlag = BATTERY_FLAG_UNKNOWN;
                BatteryLifePercent = BATTERY_LIFE_PERCENT_UNKNOWN;
                SystemStatusFlag = 0;
                BatteryLifeTime = 0;
                BatteryFullLifeTime = 0;
            }
        }
    }
}
