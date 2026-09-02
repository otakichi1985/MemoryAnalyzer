using MemoryAnalyzer.Core.Aggregation;

namespace MemoryAnalyzer.Core.History;

public static class MemoryHistorySnapshotFactory
{
    public static MemoryHistorySnapshot Create(
        DateTimeOffset capturedAt,
        IReadOnlyCollection<ApplicationSnapshot> applications,
        long physicalTotalBytes,
        long physicalAvailableBytes,
        long commitTotalBytes,
        long commitLimitBytes)
        => new(
            capturedAt,
            applications
                .OrderByDescending(application => application.WorkingSetBytes)
                .Take(32)
                .Select(application => new ApplicationMemoryHistoryPoint(
                    application.Key,
                    application.DisplayName,
                    application.WorkingSetBytes,
                    application.PrivateBytes))
                .ToArray(),
            physicalTotalBytes,
            physicalAvailableBytes,
            commitTotalBytes,
            commitLimitBytes);
}
