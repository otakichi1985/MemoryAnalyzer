namespace MemoryAnalyzer.Core.History;

public sealed record ApplicationMemoryHistorySummary(
    string Key,
    string DisplayName,
    long LatestWorkingSetBytes,
    long AverageWorkingSetBytes,
    long PeakWorkingSetBytes,
    DateTimeOffset PeakAt,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt,
    int SampleCount);
