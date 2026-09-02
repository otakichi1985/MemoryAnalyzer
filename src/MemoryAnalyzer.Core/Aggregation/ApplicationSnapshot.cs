namespace MemoryAnalyzer.Core.Aggregation;

public sealed record ApplicationSnapshot(
    string Key,
    string DisplayName,
    ApplicationCategory Category,
    int ProcessCount,
    int VisibleWindowCount,
    long WorkingSetBytes,
    long PrivateBytes,
    bool IsProtected,
    long PrivateWorkingSetBytes = 0);
