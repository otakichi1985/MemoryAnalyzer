using System.IO;
using System.Windows;
using System.Windows.Threading;
using MemoryAnalyzer.App.Update;
using MemoryAnalyzer.App.ViewModels;
using MemoryAnalyzer.Core.History;
using MemoryAnalyzer.Windows;

namespace MemoryAnalyzer.App;

public partial class MainWindow : Window
{
    private readonly DashboardViewModel _viewModel;
    private readonly UpdateService _updateService;
    private readonly DispatcherTimer _timer;
    private UpdateInfo? _pendingUpdate;

    public MainWindow()
    {
        InitializeComponent();
        var historyPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MemoryAnalyzer",
            "history-v1.jsonl");
        _viewModel = new DashboardViewModel(
            new WindowsSystemMonitor(),
            new MemoryHistoryStore(historyPath));
        _updateService = new UpdateService();
        DataContext = _viewModel;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _timer.Tick += async (_, _) => await _viewModel.RefreshAsync();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        HistoryAgentLauncher.EnsureRunning();
        await _viewModel.RefreshAsync();
        _timer.Start();
        _ = CheckForUpdatesAsync();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _timer.Stop();
        _updateService.Dispose();
    }

    private async Task CheckForUpdatesAsync()
    {
        var info = await _updateService.CheckForUpdatesAsync();
        if (info is null || !info.IsNewerThan(_updateService.CurrentVersion)) return;

        _pendingUpdate = info;
        _viewModel.Update.Message = $"新しいバージョン（v{info.Version}）があります。更新すると最新版に変わります。";
        _viewModel.Update.HasUpdate = true;
    }

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingUpdate is null || _viewModel.Update.IsBusy) return;
        _viewModel.Update.IsBusy = true;
        _viewModel.Update.ProgressText = "準備中…";
        try
        {
            var info = _pendingUpdate;
            var updatesDir = Path.Combine(Path.GetTempPath(), "MemoryAnalyzerUpdater");
            Directory.CreateDirectory(updatesDir);
            var zipPath = Path.Combine(updatesDir, $"MemoryAnalyzer-{info.Version}.zip");
            var stageDir = Path.Combine(updatesDir, $"stage-{info.Version}");
            if (Directory.Exists(stageDir)) Directory.Delete(stageDir, true);

            var progress = new Progress<int>(percent => _viewModel.Update.ProgressText = $"ダウンロード中 {percent}%");
            await _updateService.DownloadAsync(info, zipPath, progress);
            _viewModel.Update.ProgressText = "内容を確認中…";
            await _updateService.VerifyChecksumAsync(info, zipPath);
            _updateService.ExtractToDirectory(zipPath, stageDir);

            _updateService.StartUpdate(stageDir, AppContext.BaseDirectory, "MemoryAnalyzer.App.exe");
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception exception)
        {
            _viewModel.Update.IsBusy = false;
            _viewModel.Update.ProgressText = "";
            _viewModel.Update.Message = $"更新に失敗しました: {exception.Message}";
        }
    }

    private void DismissUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        _pendingUpdate = null;
        _viewModel.Update.HasUpdate = false;
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        => await _viewModel.RefreshAsync();

}
