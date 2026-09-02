namespace MemoryAnalyzer.Core.History;

public sealed record MemoryPressureHistorySummary(
    double MaximumCommitPercent,
    double MinimumAvailablePercent,
    int CriticalSampleCount,
    int SevereSampleCount,
    DateTimeOffset? MostRecentCriticalAt,
    int SampleCount = 0)
{
    public bool HasCriticalPressure => CriticalSampleCount > 0;
}
