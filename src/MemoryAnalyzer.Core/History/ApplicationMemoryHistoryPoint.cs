namespace MemoryAnalyzer.Core.History;

public sealed record ApplicationMemoryHistoryPoint(
    string Key,
    string DisplayName,
    long WorkingSetBytes,
    long PrivateBytes);
