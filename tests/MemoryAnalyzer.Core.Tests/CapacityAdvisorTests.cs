using MemoryAnalyzer.Core.Analysis;
using MemoryAnalyzer.Core.Monitoring;
using MemoryAnalyzer.Core.History;

namespace MemoryAnalyzer.Core.Tests;

public sealed class CapacityAdvisorTests
{
    private const long Gib = 1024L * 1024 * 1024;

    [Fact]
    public void Assess_DoesNotRecommendUpgradeWhilePhysicalMemoryIsAvailable()
    {
        var memory = new SystemMemorySnapshot(32 * Gib, 12 * Gib, 40 * Gib, 48 * Gib);

        var result = new CapacityAdvisor().Assess(memory, TimeSpan.Zero, 8 * Gib);

        Assert.Equal(CapacityStatus.Comfortable, result.Status);
        Assert.Contains("急ぐ", result.Title);
    }

    [Fact]
    public void Assess_WaitsBeforeCallingTransientPressureAnUpgradeCase()
    {
        var memory = new SystemMemorySnapshot(32 * Gib, 2 * Gib, 46 * Gib, 50 * Gib);

        var result = new CapacityAdvisor().Assess(memory, TimeSpan.FromMinutes(2), 2 * Gib);

        Assert.Equal(CapacityStatus.Observe, result.Status);
        Assert.Contains("確認中", result.Title);
    }

    [Fact]
    public void Assess_DoesNotWaitWhenCommitIsAlmostExhausted()
    {
        var memory = new SystemMemorySnapshot(32 * Gib, 850L * 1024 * 1024, 55_600L * 1024 * 1024, 57_000L * 1024 * 1024);

        var result = new CapacityAdvisor().Assess(memory, TimeSpan.Zero, 14 * Gib);

        Assert.Equal(CapacityStatus.CriticalAction, result.Status);
        Assert.Contains("すぐに", result.Title);
    }

    [Fact]
    public void Snapshot_ReportsCommitHeadroom()
    {
        var memory = new SystemMemorySnapshot(32 * Gib, 8 * Gib, 40 * Gib, 52 * Gib);

        Assert.Equal(12 * Gib, memory.CommitHeadroomBytes);
    }

    [Fact]
    public void Assess_UsesHistoricalCriticalPressureWhenCurrentUsageRecovered()
    {
        var memory = new SystemMemorySnapshot(32 * Gib, 14 * Gib, 40 * Gib, 52 * Gib);
        var history = new MemoryPressureHistorySummary(
            97.5,
            2.7,
            3,
            5,
            DateTimeOffset.Parse("2026-08-30T14:10:00+09:00"));

        var result = new CapacityAdvisor().Assess(memory, TimeSpan.Zero, 2 * Gib, history);

        Assert.Equal(CapacityStatus.ConsiderUpgrade, result.Status);
        Assert.Equal("RAMの増設をおすすめします", result.Title);
    }

    [Fact]
    public void Assess_PrefersAppActionsWhenTheyCanRecoverMeaningfulMemory()
    {
        var memory = new SystemMemorySnapshot(32 * Gib, 2 * Gib, 46 * Gib, 50 * Gib);

        var result = new CapacityAdvisor().Assess(memory, TimeSpan.FromMinutes(6), 7 * Gib);

        Assert.Equal(CapacityStatus.AppActionFirst, result.Status);
    }

    [Fact]
    public void Assess_ConsidersUpgradeAfterSustainedPressureWithoutLargeAppCandidates()
    {
        var memory = new SystemMemorySnapshot(32 * Gib, 2 * Gib, 46 * Gib, 50 * Gib);

        var result = new CapacityAdvisor().Assess(memory, TimeSpan.FromMinutes(6), 2 * Gib);

        Assert.Equal(CapacityStatus.ConsiderUpgrade, result.Status);
        Assert.Contains("増設", result.Title);
    }
}
