namespace MemoryAnalyzer.Core.History;

public sealed record MemoryHistorySnapshot(
    DateTimeOffset CapturedAt,
    IReadOnlyList<ApplicationMemoryHistoryPoint> Applications,
    long PhysicalTotalBytes = 0,
    long PhysicalAvailableBytes = 0,
    long CommitTotalBytes = 0,
    long CommitLimitBytes = 0);
