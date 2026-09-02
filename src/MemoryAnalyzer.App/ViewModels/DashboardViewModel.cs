using System.Collections.ObjectModel;
using MemoryAnalyzer.Core.Aggregation;
using MemoryAnalyzer.Core.Analysis;
using MemoryAnalyzer.Core.History;
using MemoryAnalyzer.Core.Monitoring;

namespace MemoryAnalyzer.App.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    private sealed record AnalyzedApplication(ApplicationSnapshot Application, ApplicationTrend Trend, ApplicationAdvice Advice, ApplicationRowViewModel Row);

    private readonly ISystemMonitor _monitor;
    private readonly MemoryHistoryStore _persistentHistory;
    private readonly ApplicationAggregator _aggregator = new();
    private readonly RecommendationEngine _recommendationEngine = new();
    private readonly CapacityAdvisor _capacityAdvisor = new();
    private readonly MemoryCompositionCalculator _compositionCalculator = new();
    private readonly MemoryHistoryTracker _history = new();
    private DateTimeOffset? _highPressureStartedAt;
    private bool _isRefreshing;
    private string _availableMemoryText = "--";
    private string _memoryDetailText = "取得中";
    private string _commitPercentText = "--";
    private string _commitHeadroomText = "残り --";
    private string _pressureLabel = "計測中";
    private string _pressureDescription = "Windowsから現在の状態を読み取っています。";
    private string _lastUpdatedText = "未更新";
    private string _observationText = "最初の傾向が分かるまで少し待ってください。";
    private double _physicalUsedPercent;
    private double _commitUsedPercent;
    private string _displayedTotalText = "--";
    private string _allProcessesText = "--";
    private string _windowsProcessesText = "--";
    private string _kernelMemoryText = "--";
    private string _kernelDetailText = "--";
    private string _systemCacheText = "--";
    private string _capacityTitle = "計測中";
    private string _capacityDescription = "RAM増設が必要か判断するため、現在の状態を確認しています。";
    private string _capacityKey = CapacityStatus.Observe.ToString();
    private string _historyStatusText = "過去の記録を読み込んでいます。";
    private string _capacityHistoryEvidence = "過去データを収集中";
    private MemoryPressureHistorySummary? _pressureHistory;
    private DateTimeOffset _nextHistoryRefreshAt = DateTimeOffset.MinValue;
    private double _availableSegmentWidth;
    private double _applicationSegmentWidth;
    private double _windowsSegmentWidth;
    private double _kernelSegmentWidth;
    private double _otherSegmentWidth;
    private string _availableSegmentText = "--";
    private string _applicationSegmentText = "--";
    private string _windowsSegmentText = "--";
    private string _kernelSegmentText = "--";
    private string _otherSegmentText = "--";

    public DashboardViewModel(ISystemMonitor monitor, MemoryHistoryStore persistentHistory)
    {
        _monitor = monitor;
        _persistentHistory = persistentHistory;
    }

    public ObservableCollection<ApplicationRowViewModel> Applications { get; } = [];
    public ObservableCollection<HistorySummaryViewModel> HistoryHighlights { get; } = [];
    public UpdateViewModel Update { get; } = new();

    public string AvailableMemoryText { get => _availableMemoryText; private set { _availableMemoryText = value; OnPropertyChanged(); } }
    public string MemoryDetailText { get => _memoryDetailText; private set { _memoryDetailText = value; OnPropertyChanged(); } }
    public string CommitPercentText { get => _commitPercentText; private set { _commitPercentText = value; OnPropertyChanged(); } }
    public string CommitHeadroomText { get => _commitHeadroomText; private set { _commitHeadroomText = value; OnPropertyChanged(); } }
    public string PressureLabel { get => _pressureLabel; private set { _pressureLabel = value; OnPropertyChanged(); } }
    public string PressureDescription { get => _pressureDescription; private set { _pressureDescription = value; OnPropertyChanged(); } }
    public string LastUpdatedText { get => _lastUpdatedText; private set { _lastUpdatedText = value; OnPropertyChanged(); } }
    public string ObservationText { get => _observationText; private set { _observationText = value; OnPropertyChanged(); } }
    public double PhysicalUsedPercent { get => _physicalUsedPercent; private set { _physicalUsedPercent = value; OnPropertyChanged(); } }
    public double CommitUsedPercent { get => _commitUsedPercent; private set { _commitUsedPercent = value; OnPropertyChanged(); } }
    public string DisplayedTotalText { get => _displayedTotalText; private set { _displayedTotalText = value; OnPropertyChanged(); } }
    public string AllProcessesText { get => _allProcessesText; private set { _allProcessesText = value; OnPropertyChanged(); } }
    public string WindowsProcessesText { get => _windowsProcessesText; private set { _windowsProcessesText = value; OnPropertyChanged(); } }
    public string KernelMemoryText { get => _kernelMemoryText; private set { _kernelMemoryText = value; OnPropertyChanged(); } }
    public string KernelDetailText { get => _kernelDetailText; private set { _kernelDetailText = value; OnPropertyChanged(); } }
    public string SystemCacheText { get => _systemCacheText; private set { _systemCacheText = value; OnPropertyChanged(); } }
    public string CapacityTitle { get => _capacityTitle; private set { _capacityTitle = value; OnPropertyChanged(); } }
    public string CapacityDescription { get => _capacityDescription; private set { _capacityDescription = value; OnPropertyChanged(); } }
    public string CapacityKey { get => _capacityKey; private set { _capacityKey = value; OnPropertyChanged(); } }
    public string HistoryStatusText { get => _historyStatusText; private set { _historyStatusText = value; OnPropertyChanged(); } }
    public string CapacityHistoryEvidence { get => _capacityHistoryEvidence; private set { _capacityHistoryEvidence = value; OnPropertyChanged(); } }
    public double AvailableSegmentWidth { get => _availableSegmentWidth; private set { _availableSegmentWidth = value; OnPropertyChanged(); } }
    public double ApplicationSegmentWidth { get => _applicationSegmentWidth; private set { _applicationSegmentWidth = value; OnPropertyChanged(); } }
    public double WindowsSegmentWidth { get => _windowsSegmentWidth; private set { _windowsSegmentWidth = value; OnPropertyChanged(); } }
    public double KernelSegmentWidth { get => _kernelSegmentWidth; private set { _kernelSegmentWidth = value; OnPropertyChanged(); } }
    public double OtherSegmentWidth { get => _otherSegmentWidth; private set { _otherSegmentWidth = value; OnPropertyChanged(); } }
    public string AvailableSegmentText { get => _availableSegmentText; private set { _availableSegmentText = value; OnPropertyChanged(); } }
    public string ApplicationSegmentText { get => _applicationSegmentText; private set { _applicationSegmentText = value; OnPropertyChanged(); } }
    public string WindowsSegmentText { get => _windowsSegmentText; private set { _windowsSegmentText = value; OnPropertyChanged(); } }
    public string KernelSegmentText { get => _kernelSegmentText; private set { _kernelSegmentText = value; OnPropertyChanged(); } }
    public string OtherSegmentText { get => _otherSegmentText; private set { _otherSegmentText = value; OnPropertyChanged(); } }

    public async Task RefreshAsync()
    {
        if (_isRefreshing) return;
        _isRefreshing = true;
        try
        {
            var snapshot = await Task.Run(_monitor.Capture);
            var applications = _aggregator.Aggregate(snapshot.Processes);
            if (snapshot.CapturedAt >= _nextHistoryRefreshAt)
            {
                var summaries = await Task.Run(() => _persistentHistory.ReadSummaries(snapshot.CapturedAt));
                _pressureHistory = await Task.Run(() => _persistentHistory.ReadPressureSummary(snapshot.CapturedAt));
                _nextHistoryRefreshAt = snapshot.CapturedAt.AddMinutes(1);
                HistoryHighlights.Clear();
                foreach (var summary in summaries.Take(8))
                {
                    HistoryHighlights.Add(new HistorySummaryViewModel(summary));
                }
                HistoryStatusText = summaries.Count == 0
                    ? "記録開始直後です。1分ごとに保存します。"
                    : $"過去7日分を記録中（{summaries.Count}アプリ、1分ごと）";
            }
            var analyzed = applications
                .Select(application =>
                {
                    var trend = _history.Record(application.Key, snapshot.CapturedAt, application.WorkingSetBytes);
                    var advice = _recommendationEngine.Analyze(application, trend, snapshot.SystemMemory);
                    return new AnalyzedApplication(
                        application,
                        trend,
                        advice,
                        new ApplicationRowViewModel(application, trend, advice, snapshot.SystemMemory.PhysicalTotalBytes));
                })
                .ToArray();
            var rows = analyzed
                .Select(item => item.Row)
                .OrderByDescending(row => row.SortScore)
                .ThenByDescending(row => row.MemorySharePercent)
                .Take(16)
                .ToArray();

            RefreshApplicationRows(rows, analyzed, snapshot.SystemMemory.PhysicalTotalBytes);
            var actionableWorkingSet = analyzed
                .Where(item => !item.Application.IsProtected && item.Advice.AttentionScore >= 55)
                .Sum(item => item.Application.WorkingSetBytes);
            ApplySystemSummary(
                snapshot,
                applications,
                rows,
                actionableWorkingSet);
        }
        catch (Exception exception)
        {
            PressureLabel = "取得できませんでした";
            PressureDescription = $"監視を続けられません: {exception.Message}";
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private void RefreshApplicationRows(
        IReadOnlyList<ApplicationRowViewModel> desiredRows,
        IReadOnlyList<AnalyzedApplication> analyzed,
        long physicalTotalBytes)
    {
        var analysisByKey = analyzed.ToDictionary(item => item.Application.Key, StringComparer.OrdinalIgnoreCase);
        var existingByKey = Applications.ToDictionary(row => row.Key, StringComparer.OrdinalIgnoreCase);
        var desiredKeys = desiredRows.Select(row => row.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var index = Applications.Count - 1; index >= 0; index--)
        {
            if (!desiredKeys.Contains(Applications[index].Key)) Applications.RemoveAt(index);
        }

        for (var targetIndex = 0; targetIndex < desiredRows.Count; targetIndex++)
        {
            var desired = desiredRows[targetIndex];
            if (!existingByKey.TryGetValue(desired.Key, out var row))
            {
                Applications.Insert(targetIndex, desired);
                continue;
            }

            var item = analysisByKey[desired.Key];
            row.Update(item.Application, item.Trend, item.Advice, physicalTotalBytes);
            var currentIndex = Applications.IndexOf(row);
            if (currentIndex != targetIndex) Applications.Move(currentIndex, targetIndex);
        }
    }

    private void ApplySystemSummary(
        MonitoringSnapshot snapshot,
        IReadOnlyCollection<ApplicationSnapshot> applications,
        IReadOnlyCollection<ApplicationRowViewModel> rows,
        long actionableWorkingSetBytes)
    {
        var memory = snapshot.SystemMemory;
        AvailableMemoryText = FormatBytes(memory.PhysicalAvailableBytes);
        MemoryDetailText = $"{FormatBytes(memory.PhysicalTotalBytes - memory.PhysicalAvailableBytes)} 使用中 / 合計 {FormatBytes(memory.PhysicalTotalBytes)}";
        CommitPercentText = $"{memory.CommitUsedPercent:0}%";
        CommitHeadroomText = $"新しく確保できる残り {FormatBytes(memory.CommitHeadroomBytes)}";
        PhysicalUsedPercent = memory.PhysicalUsedPercent;
        CommitUsedPercent = memory.CommitUsedPercent;
        LastUpdatedText = $"最終更新 {snapshot.CapturedAt:HH:mm:ss}";

        DisplayedTotalText = FormatBytes(rows.Sum(row => row.WorkingSetBytes));
        AllProcessesText = FormatBytes(snapshot.Processes.Sum(process => process.WorkingSetBytes));
        WindowsProcessesText = FormatBytes(applications.Where(application => application.IsProtected).Sum(application => application.WorkingSetBytes));
        KernelMemoryText = FormatBytes(memory.KernelTotalBytes);
        KernelDetailText = $"常駐 {FormatBytes(memory.KernelNonPagedBytes)} / ページ可能 {FormatBytes(memory.KernelPagedBytes)}";
        SystemCacheText = FormatBytes(memory.SystemCacheBytes);

        var composition = _compositionCalculator.Calculate(memory, applications);
        const double gaugeWidth = 276;
        AvailableSegmentWidth = SegmentWidth(composition.AvailableBytes, composition.TotalBytes, gaugeWidth);
        ApplicationSegmentWidth = SegmentWidth(composition.ApplicationPrivateBytes, composition.TotalBytes, gaugeWidth);
        WindowsSegmentWidth = SegmentWidth(composition.WindowsPrivateBytes, composition.TotalBytes, gaugeWidth);
        KernelSegmentWidth = SegmentWidth(composition.KernelBytes, composition.TotalBytes, gaugeWidth);
        OtherSegmentWidth = SegmentWidth(composition.SharedCacheAndOtherBytes, composition.TotalBytes, gaugeWidth);
        AvailableSegmentText = $"空き・再利用可能 {FormatBytes(composition.AvailableBytes)}";
        ApplicationSegmentText = $"動作中アプリ専用 {FormatBytes(composition.ApplicationPrivateBytes)}";
        WindowsSegmentText = $"Windowsプロセス専用 {FormatBytes(composition.WindowsPrivateBytes)}";
        KernelSegmentText = $"カーネル等 {FormatBytes(composition.KernelBytes)}";
        OtherSegmentText = $"共有・キャッシュ等 {FormatBytes(composition.SharedCacheAndOtherBytes)}";

        var severePressure = memory.PhysicalAvailablePercent < 10 && memory.CommitUsedPercent >= 90;
        if (severePressure)
        {
            _highPressureStartedAt ??= snapshot.CapturedAt;
        }
        else
        {
            _highPressureStartedAt = null;
        }

        var pressureDuration = _highPressureStartedAt is null
            ? TimeSpan.Zero
            : snapshot.CapturedAt - _highPressureStartedAt.Value;
        var capacity = _capacityAdvisor.Assess(memory, pressureDuration, actionableWorkingSetBytes, _pressureHistory);
        CapacityTitle = capacity.Title;
        CapacityDescription = capacity.Description;
        CapacityKey = capacity.Status.ToString();
        CapacityHistoryEvidence = _pressureHistory is { SampleCount: > 0 }
            ? $"過去7日の記録: 最大コミット {_pressureHistory.MaximumCommitPercent:0}% / 最小空き {_pressureHistory.MinimumAvailablePercent:0}%"
            : "過去データを収集中（1分ごと）";

        (PressureLabel, PressureDescription) = memory.CommitUsedPercent switch
        {
            >= 97 => ("今すぐ対処が必要", $"新しく確保できる残りは {FormatBytes(memory.CommitHeadroomBytes)} です。保存して重いアプリを閉じてください。"),
            >= 90 => ("かなり余裕が少ない", "コミットが90%を超えています。「確認してから実行」の候補を見直してください。"),
            >= 80 => ("余裕が少なめ", "すぐ危険ではありませんが、増加中のアプリを優先して確認してください。"),
            >= 65 => ("やや高め", "重い作業中なら正常な範囲です。増加が続くアプリだけ確認してください。"),
            _ => ("余裕あり", "メモリ不足の兆候は見つかっていません。無理に終了する必要はありません。")
        };

        var urgent = rows.FirstOrDefault(row => row.AttentionScore >= 80);
        ObservationText = urgent is null
            ? "急いで対処する項目はありません。"
            : $"まず「{urgent.DisplayName}」を確認してください。{urgent.Action}のが効果的です。";
    }

    private static string FormatBytes(long bytes) => bytes >= 1024L * 1024 * 1024
        ? $"{bytes / (1024d * 1024 * 1024):0.0} GB"
        : $"{bytes / (1024d * 1024):0} MB";

    private static double SegmentWidth(long bytes, long totalBytes, double gaugeWidth) => totalBytes <= 0
        ? 0
        : Math.Max(0, (double)bytes / totalBytes * gaugeWidth);
}
