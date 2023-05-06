using EnergyStar.Interop;

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

        static void OnBypassListChanged(object sender, FileSystemEventArgs e)
        {
            var path = e.FullPath;
            if (path == null)
                return;

            LoadBypassList(path);
        }

        static void LoadBypassList(string path)
        {
            if (File.Exists(path))
            {
                var file = File.OpenText(path);
                var content = file.ReadToEnd();
                file.Close();

                if (content != null)
                {
                    var srcBypassList = content.Replace("\r\n", "\n").Split('\n');
                    var bypassList = new List<string> { };
                    // Remove empty items
                    foreach (var item in srcBypassList)
                    {
                        var trimed = item.Trim().ToLowerInvariant();
                        if (trimed.Length > 0)
                        {
                            bypassList.Add(trimed);
                        }
                    }

                    if (bypassList != null)
                    {
                        EnergyManager.SetBypassProcessList(bypassList);
                        Console.WriteLine("Bypass list updated");
                    }
                }
            }
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

            // Load bypass list and watch for changes.
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var bypassListFile = Path.Combine(baseDir, "bypass.txt");
            using var bypassListWatcher = new FileSystemWatcher(baseDir);

            bypassListWatcher.Changed += OnBypassListChanged;
            bypassListWatcher.Created += OnBypassListChanged;
            bypassListWatcher.Deleted += OnBypassListChanged;
            bypassListWatcher.Renamed += OnBypassListChanged;

            bypassListWatcher.Filter = "bypass.txt";
            bypassListWatcher.IncludeSubdirectories = true;
            bypassListWatcher.EnableRaisingEvents = true;

            LoadBypassList(bypassListFile);


            HookManager.SubscribeToWindowEvents();
            EnergyManager.ThrottleAllUserBackgroundProcesses();

            var houseKeepingThread = new Thread(new ThreadStart(HouseKeepingThreadProc));
            houseKeepingThread.Start();

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
