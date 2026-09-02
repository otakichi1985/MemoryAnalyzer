using System.Diagnostics;
using MemoryAnalyzer.Core.Aggregation;
using MemoryAnalyzer.Core.History;
using MemoryAnalyzer.Windows;

namespace MemoryAnalyzer.Agent;

internal static class Program
{
    private const string MutexName = "Local\\MemoryAnalyzer.Agent.Singleton";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew) return;

        ApplicationConfiguration.Initialize();
        using var agent = new HistoryAgent();
        agent.Run();
    }
}

internal sealed class HistoryAgent : IDisposable
{
    private readonly WindowsSystemMonitor _monitor = new();
    private readonly ApplicationAggregator _aggregator = new();
    private readonly MemoryHistoryStore _history;
    private readonly System.Windows.Forms.NotifyIcon _trayIcon;
    private readonly System.Windows.Forms.Timer _timer;
    private bool _capturing;

    public HistoryAgent()
    {
        var historyPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MemoryAnalyzer",
            "history-v1.jsonl");
        _history = new MemoryHistoryStore(historyPath);
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Information,
            Text = "Memory Analyzer - 履歴を記録中",
            Visible = true,
            ContextMenuStrip = CreateMenu()
        };
        _trayIcon.DoubleClick += (_, _) => OpenDashboard();
        _timer = new System.Windows.Forms.Timer { Interval = 60_000 };
        _timer.Tick += async (_, _) => await CaptureAsync();
    }

    public void Run()
    {
        _timer.Start();
        _ = CaptureAsync();
        System.Windows.Forms.Application.Run();
    }

    private System.Windows.Forms.ContextMenuStrip CreateMenu()
    {
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Memory Analyzerを開く", null, (_, _) => OpenDashboard());
        menu.Items.Add("履歴の記録を終了", null, (_, _) => System.Windows.Forms.Application.Exit());
        return menu;
    }

    private async Task CaptureAsync()
    {
        if (_capturing) return;
        _capturing = true;
        try
        {
            await Task.Run(() =>
            {
                var snapshot = _monitor.Capture();
                var applications = _aggregator.Aggregate(snapshot.Processes);
                _history.TryRecord(MemoryHistorySnapshotFactory.Create(
                    snapshot.CapturedAt,
                    applications,
                    snapshot.SystemMemory.PhysicalTotalBytes,
                    snapshot.SystemMemory.PhysicalAvailableBytes,
                    snapshot.SystemMemory.CommitTotalBytes,
                    snapshot.SystemMemory.CommitLimitBytes));
            });
        }
        finally
        {
            _capturing = false;
        }
    }

    private static void OpenDashboard()
    {
        var appPath = Path.Combine(AppContext.BaseDirectory, "MemoryAnalyzer.App.exe");
        if (!File.Exists(appPath)) return;
        if (Process.GetProcessesByName("MemoryAnalyzer.App").Length == 0)
        {
            Process.Start(new ProcessStartInfo(appPath) { UseShellExecute = true });
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
        _trayIcon.Dispose();
    }
}
