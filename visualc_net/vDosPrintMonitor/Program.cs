namespace vDosPrintMonitor;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        using var mutex = new Mutex(initiallyOwned: true, name: @"Global\vDosPrintMonitor", createdNew: out var createdNew);
        if (!createdNew)
            return;

        ApplicationConfiguration.Initialize();
        Application.Run(new PrintMonitorContext(args));
    }
}

internal sealed class PrintMonitorContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly SynchronizationContext _syncContext;
    private readonly Dictionary<string, long> _positions = new(StringComparer.OrdinalIgnoreCase);
    private readonly System.Windows.Forms.Timer _debounceTimer = new() { Interval = 750 };
    private readonly System.Windows.Forms.Timer _startupStatusTimer = new() { Interval = 1200 };
    private FileSystemWatcher? _watcher;
    private string _watchFolder;
    private string? _lastNotificationKey;

    public PrintMonitorContext(string[] args)
    {
        _syncContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _watchFolder = ResolveWatchFolder(args);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Text = "vDos Print Monitor",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };

        _debounceTimer.Tick += (_, _) =>
        {
            _debounceTimer.Stop();
            CheckLogs(showQuietStatus: false);
        };
        _startupStatusTimer.Tick += (_, _) =>
        {
            _startupStatusTimer.Stop();
            ShowLastKnownStatus();
        };

        StartWatching();
        InitializeLogPositions();
        ShowNotification("vDos Print Monitor", $"Watching {_watchFolder}", ToolTipIcon.Info);
        _startupStatusTimer.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _debounceTimer.Dispose();
            _startupStatusTimer.Dispose();
            _watcher?.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        base.Dispose(disposing);
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Check print status", null, (_, _) => CheckLogs(showQuietStatus: true));
        menu.Items.Add("Open vDos output folder", null, (_, _) => OpenFolder(_watchFolder));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());
        return menu;
    }

    private void StartWatching()
    {
        Directory.CreateDirectory(_watchFolder);
        _watcher = new FileSystemWatcher(_watchFolder)
        {
            Filter = "#LPT*.tcp.log",
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };

        _watcher.Created += (_, _) => DebouncedCheck();
        _watcher.Changed += (_, _) => DebouncedCheck();
        _watcher.Renamed += (_, _) => DebouncedCheck();
        _watcher.Error += (_, e) => ShowNotification("vDos Print Monitor", e.GetException().Message, ToolTipIcon.Warning);
    }

    private void DebouncedCheck()
    {
        _syncContext.Post(_ =>
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }, null);
    }

    private void InitializeLogPositions()
    {
        foreach (var path in Directory.EnumerateFiles(_watchFolder, "#LPT*.tcp.log"))
        {
            try
            {
                _positions[path] = new FileInfo(path).Length;
            }
            catch
            {
                _positions[path] = 0;
            }
        }
    }

    private void CheckLogs(bool showQuietStatus)
    {
        var foundNewStatus = false;

        foreach (var path in Directory.EnumerateFiles(_watchFolder, "#LPT*.tcp.log"))
        {
            foreach (var line in ReadNewLines(path))
            {
                var status = PrintLogStatus.TryParse(path, line);
                if (status == null)
                    continue;

                foundNewStatus = true;
                NotifyStatus(status);
            }
        }

        if (showQuietStatus && !foundNewStatus)
            ShowLastKnownStatus();
    }

    private IEnumerable<string> ReadNewLines(string path)
    {
        long start = _positions.TryGetValue(path, out var knownPosition) ? knownPosition : 0;
        var length = new FileInfo(path).Length;
        if (length < start)
            start = 0;

        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Seek(start, SeekOrigin.Begin);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = reader.ReadLine()) != null)
            yield return line;

        _positions[path] = stream.Position;
    }

    private void ShowLastKnownStatus()
    {
        var newest = Directory.EnumerateFiles(_watchFolder, "#LPT*.tcp.log")
            .SelectMany(path => ReadAllStatusLines(path))
            .OrderByDescending(status => status.Timestamp)
            .FirstOrDefault();

        if (newest == null)
        {
            ShowNotification("vDos Print Monitor", "No TCP print status has been logged yet.", ToolTipIcon.Info);
            return;
        }

        NotifyStatus(newest, force: true);
    }

    private static IEnumerable<PrintLogStatus> ReadAllStatusLines(string path)
    {
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var status = PrintLogStatus.TryParse(path, line);
            if (status != null)
                yield return status;
        }
    }

    private void NotifyStatus(PrintLogStatus status, bool force = false)
    {
        var key = $"{status.Timestamp:o}|{status.Port}|{status.Result}|{status.Detail}";
        if (!force && key == _lastNotificationKey)
            return;

        _lastNotificationKey = key;
        var success = status.Result.Equals("sent", StringComparison.OrdinalIgnoreCase);
        var icon = success ? ToolTipIcon.Info : ToolTipIcon.Error;
        var title = success ? $"{status.Port} print sent" : $"{status.Port} print failed";
        var message = $"{status.Target} bytes={status.Bytes}";
        if (!string.IsNullOrWhiteSpace(status.Detail))
            message += Environment.NewLine + status.Detail;

        ShowNotification(title, message, icon);
    }

    private void ShowNotification(string title, string message, ToolTipIcon icon)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = icon;
        _notifyIcon.ShowBalloonTip(7000);
    }

    private static void OpenFolder(string folder)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = folder,
            UseShellExecute = true
        });
    }

    private static string ResolveWatchFolder(string[] args)
    {
        if (args.Length > 0 && Directory.Exists(args[0]))
            return Path.GetFullPath(args[0]);

        var baseDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (File.Exists(Path.Combine(baseDirectory, "vDos.exe")))
            return baseDirectory;

        var current = new DirectoryInfo(baseDirectory);
        while (current != null)
        {
            if (current.Name.Equals("visualc_net", StringComparison.OrdinalIgnoreCase))
                return PreferExistingFolder(Path.Combine(current.FullName, "Debug"), Path.Combine(current.FullName, "Release"));

            if (current.Parent?.Name.Equals("visualc_net", StringComparison.OrdinalIgnoreCase) == true)
                return PreferExistingFolder(Path.Combine(current.Parent.FullName, "Debug"), Path.Combine(current.Parent.FullName, "Release"));

            current = current.Parent;
        }

        return baseDirectory;
    }

    private static string PreferExistingFolder(string primary, string fallback)
    {
        if (Directory.Exists(primary))
            return primary;

        return Directory.Exists(fallback) ? fallback : primary;
    }
}

internal sealed class PrintLogStatus
{
    public DateTime Timestamp { get; init; }
    public string Port { get; init; } = "";
    public string Target { get; init; } = "";
    public int Bytes { get; init; }
    public string Result { get; init; } = "";
    public string Detail { get; init; } = "";

    public static PrintLogStatus? TryParse(string path, string line)
    {
        if (!line.Contains(" result=", StringComparison.OrdinalIgnoreCase))
            return null;

        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 7)
            return null;

        if (!DateTime.TryParse($"{parts[0]} {parts[1]}", out var timestamp))
            timestamp = File.GetLastWriteTime(path);

        var port = parts[2];
        var target = parts.Length > 4 ? parts[4] : "";
        var bytes = ReadIntToken(line, "bytes=");
        var result = ReadToken(line, "result=");
        var detail = ReadTokenRemainder(line, "detail=");

        return new PrintLogStatus
        {
            Timestamp = timestamp,
            Port = port,
            Target = target,
            Bytes = bytes,
            Result = string.IsNullOrWhiteSpace(result) ? "unknown" : result,
            Detail = detail
        };
    }

    private static int ReadIntToken(string line, string name)
    {
        return int.TryParse(ReadToken(line, name), out var value) ? value : 0;
    }

    private static string ReadToken(string line, string name)
    {
        var start = line.IndexOf(name, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return "";

        start += name.Length;
        var end = line.IndexOf(' ', start);
        return end < 0 ? line[start..] : line[start..end];
    }

    private static string ReadTokenRemainder(string line, string name)
    {
        var start = line.IndexOf(name, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return "";

        return line[(start + name.Length)..];
    }
}
