using MemoryAnalyzer.Core.History;

namespace MemoryAnalyzer.App.ViewModels;

public sealed class HistorySummaryViewModel
{
    public HistorySummaryViewModel(ApplicationMemoryHistorySummary summary)
    {
        DisplayName = summary.DisplayName;
        PeakText = $"最大 {FormatBytes(summary.PeakWorkingSetBytes)}";
        AverageText = $"平均 {FormatBytes(summary.AverageWorkingSetBytes)} / 最近 {FormatBytes(summary.LatestWorkingSetBytes)}";
        PeakAtText = $"{summary.PeakAt:M/d HH:mm} に最大";
    }

    public string DisplayName { get; }
    public string PeakText { get; }
    public string AverageText { get; }
    public string PeakAtText { get; }

    private static string FormatBytes(long bytes) => bytes >= 1024L * 1024 * 1024
        ? $"{bytes / (1024d * 1024 * 1024):0.00} GB"
        : $"{bytes / (1024d * 1024):0} MB";
}
