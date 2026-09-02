using MemoryAnalyzer.Core.Aggregation;
using MemoryAnalyzer.Core.Analysis;

namespace MemoryAnalyzer.App.ViewModels;

public sealed class ApplicationRowViewModel : ObservableObject
{
    private string _processSummary = string.Empty;
    private string _workingSetText = string.Empty;
    private long _workingSetBytes;
    private string _privateText = string.Empty;
    private double _memorySharePercent;
    private string _trendText = string.Empty;
    private string _reason = string.Empty;
    private string _action = string.Empty;
    private string _effect = string.Empty;
    private string _sideEffect = string.Empty;
    private string _safetyLabel = string.Empty;
    private string _safetyKey = string.Empty;
    private int _attentionScore;
    private double _sortScore;

    public ApplicationRowViewModel(ApplicationSnapshot application, ApplicationTrend trend, ApplicationAdvice advice, long physicalTotalBytes)
    {
        Key = application.Key;
        DisplayName = application.DisplayName;
        Initial = application.DisplayName.Length > 0 ? application.DisplayName[..1].ToUpperInvariant() : "?";
        Update(application, trend, advice, physicalTotalBytes);
    }

    public string Key { get; }
    public string DisplayName { get; }
    public string Initial { get; }
    public string ProcessSummary { get => _processSummary; private set => SetProperty(ref _processSummary, value); }
    public string WorkingSetText { get => _workingSetText; private set => SetProperty(ref _workingSetText, value); }
    public long WorkingSetBytes { get => _workingSetBytes; private set => SetProperty(ref _workingSetBytes, value); }
    public string PrivateText { get => _privateText; private set => SetProperty(ref _privateText, value); }
    public double MemorySharePercent { get => _memorySharePercent; private set => SetProperty(ref _memorySharePercent, value); }
    public string TrendText { get => _trendText; private set => SetProperty(ref _trendText, value); }
    public string Reason { get => _reason; private set => SetProperty(ref _reason, value); }
    public string Action { get => _action; private set => SetProperty(ref _action, value); }
    public string Effect { get => _effect; private set => SetProperty(ref _effect, value); }
    public string SideEffect { get => _sideEffect; private set => SetProperty(ref _sideEffect, value); }
    public string SafetyLabel { get => _safetyLabel; private set => SetProperty(ref _safetyLabel, value); }
    public string SafetyKey { get => _safetyKey; private set => SetProperty(ref _safetyKey, value); }
    public int AttentionScore { get => _attentionScore; private set => SetProperty(ref _attentionScore, value); }
    public double SortScore { get => _sortScore; private set => SetProperty(ref _sortScore, value); }

    public void Update(ApplicationSnapshot application, ApplicationTrend trend, ApplicationAdvice advice, long physicalTotalBytes)
    {
        ProcessSummary = $"{application.ProcessCount}個の関連プロセスをまとめて表示";
        WorkingSetBytes = application.WorkingSetBytes;
        WorkingSetText = FormatBytes(application.WorkingSetBytes);
        PrivateText = $"専用メモリ（RAM外を含む） {FormatBytes(application.PrivateBytes)}";
        MemorySharePercent = physicalTotalBytes == 0 ? 0 : Math.Clamp((double)application.WorkingSetBytes / physicalTotalBytes * 100, 0, 100);
        TrendText = trend.ObservedFor < ApplicationTrend.MinimumObservation
            ? "変化を観測中"
            : trend.ChangeBytes switch
            {
                >= 128L * 1024 * 1024 => $"↗ {FormatSignedBytes(trend.ChangeBytes)}",
                <= -128L * 1024 * 1024 => $"↘ {FormatSignedBytes(trend.ChangeBytes)}",
                _ => "→ ほぼ一定"
            };
        Reason = advice.Reason;
        Action = advice.Action;
        Effect = advice.Effect;
        SideEffect = advice.SideEffect;
        SafetyLabel = advice.Safety switch
        {
            SafetyLevel.Safe => "安全",
            SafetyLevel.CheckFirst => "確認してから実行",
            SafetyLevel.Advanced => "上級者向け",
            SafetyLevel.DoNotOperate => "操作しないでください",
            _ => "確認"
        };
        SafetyKey = advice.Safety.ToString();
        AttentionScore = advice.AttentionScore;
        SortScore = advice.Safety == SafetyLevel.DoNotOperate ? 0 : advice.AttentionScore + Math.Min(MemorySharePercent * 3, 50);
    }

    private static string FormatBytes(long bytes) => bytes >= 1024L * 1024 * 1024
        ? $"{bytes / (1024d * 1024 * 1024):0.00} GB"
        : $"{bytes / (1024d * 1024):0} MB";

    private static string FormatSignedBytes(long bytes)
    {
        var sign = bytes >= 0 ? "+" : "−";
        return $"{sign}{FormatBytes(Math.Abs(bytes))}";
    }
}
