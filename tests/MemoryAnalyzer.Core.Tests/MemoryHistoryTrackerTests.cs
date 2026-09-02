using MemoryAnalyzer.Core.Analysis;

namespace MemoryAnalyzer.Core.Tests;

public sealed class MemoryHistoryTrackerTests
{
    [Fact]
    public void Record_ReportsChangeWithinWindow()
    {
        var tracker = new MemoryHistoryTracker(TimeSpan.FromMinutes(5));
        var start = DateTimeOffset.Parse("2026-08-30T10:00:00+09:00");

        tracker.Record("app", start, 500);
        var trend = tracker.Record("app", start.AddMinutes(1), 850);

        Assert.Equal(350, trend.ChangeBytes);
        Assert.Equal(TimeSpan.FromMinutes(1), trend.ObservedFor);
    }

    [Fact]
    public void Record_DropsExpiredPoints()
    {
        var tracker = new MemoryHistoryTracker(TimeSpan.FromMinutes(1));
        var start = DateTimeOffset.Parse("2026-08-30T10:00:00+09:00");

        tracker.Record("app", start, 500);
        tracker.Record("app", start.AddSeconds(30), 700);
        var trend = tracker.Record("app", start.AddMinutes(2), 800);

        Assert.Equal(0, trend.ChangeBytes);
        Assert.Equal(TimeSpan.Zero, trend.ObservedFor);
    }
}
