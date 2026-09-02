using MemoryAnalyzer.Core.History;

namespace MemoryAnalyzer.Core.Tests;

public sealed class MemoryHistoryStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"memory-analyzer-tests-{Guid.NewGuid():N}");
    private string HistoryPath => Path.Combine(_directory, "history.jsonl");

    [Fact]
    public void Record_PersistsAcrossStoreInstancesAndBuildsSummary()
    {
        var start = DateTimeOffset.Parse("2026-08-30T10:00:00+09:00");
        var store = new MemoryHistoryStore(HistoryPath, TimeSpan.FromMinutes(1), TimeSpan.FromDays(7));

        Assert.True(store.TryRecord(Snapshot(start, 1_000)));
        Assert.True(store.TryRecord(Snapshot(start.AddMinutes(1), 3_000)));

        var reopened = new MemoryHistoryStore(HistoryPath, TimeSpan.FromMinutes(1), TimeSpan.FromDays(7));
        var summary = Assert.Single(reopened.ReadSummaries(start.AddMinutes(2)));
        Assert.Equal(3_000, summary.PeakWorkingSetBytes);
        Assert.Equal(2_000, summary.AverageWorkingSetBytes);
        Assert.Equal(start.AddMinutes(1), summary.PeakAt);
    }

    [Fact]
    public void Record_SkipsSamplesInsideInterval()
    {
        var start = DateTimeOffset.Parse("2026-08-30T10:00:00+09:00");
        var store = new MemoryHistoryStore(HistoryPath, TimeSpan.FromMinutes(1), TimeSpan.FromDays(7));

        Assert.True(store.TryRecord(Snapshot(start, 1_000)));
        Assert.False(store.TryRecord(Snapshot(start.AddSeconds(30), 2_000)));
        Assert.Single(store.ReadSummaries(start.AddMinutes(1)));
    }

    [Fact]
    public void ReadSummaries_IgnoresBrokenLines()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(HistoryPath, "not-json" + Environment.NewLine);
        var start = DateTimeOffset.Parse("2026-08-30T10:00:00+09:00");
        var store = new MemoryHistoryStore(HistoryPath, TimeSpan.Zero, TimeSpan.FromDays(7));

        Assert.True(store.TryRecord(Snapshot(start, 1_000)));

        Assert.Single(store.ReadSummaries(start));
    }

    [Fact]
    public void ReadPressureSummary_FindsHistoricalCriticalSamples()
    {
        var start = DateTimeOffset.Parse("2026-08-30T10:00:00+09:00");
        var store = new MemoryHistoryStore(HistoryPath, TimeSpan.Zero, TimeSpan.FromDays(7));
        store.TryRecord(new MemoryHistorySnapshot(
            start,
            [],
            32 * 1024L,
            850,
            55_600,
            57_000));

        var summary = store.ReadPressureSummary(start.AddMinutes(1));

        Assert.True(summary.HasCriticalPressure);
        Assert.Equal(1, summary.CriticalSampleCount);
        Assert.Equal(start, summary.MostRecentCriticalAt);
    }

    private static MemoryHistorySnapshot Snapshot(DateTimeOffset at, long bytes) => new(
        at,
        [new ApplicationMemoryHistoryPoint("app", "Test App", bytes, bytes + 100)]);

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }
}
