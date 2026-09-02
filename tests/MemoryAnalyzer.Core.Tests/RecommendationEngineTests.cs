using MemoryAnalyzer.Core.Aggregation;
using MemoryAnalyzer.Core.Analysis;
using MemoryAnalyzer.Core.Monitoring;

namespace MemoryAnalyzer.Core.Tests;

public sealed class RecommendationEngineTests
{
    private static readonly SystemMemorySnapshot NormalMemory = new(
        32L * 1024 * 1024 * 1024,
        12L * 1024 * 1024 * 1024,
        25L * 1024 * 1024 * 1024,
        48L * 1024 * 1024 * 1024);

    [Fact]
    public void Analyze_NeverRecommendsStoppingProtectedProcess()
    {
        var app = new ApplicationSnapshot("svchost", "Service Host", ApplicationCategory.System, 50, 0, 4_000_000_000, 4_000_000_000, true);

        var advice = new RecommendationEngine().Analyze(app, new ApplicationTrend(0, TimeSpan.FromMinutes(1)), NormalMemory);

        Assert.Equal(SafetyLevel.DoNotOperate, advice.Safety);
        Assert.Contains("操作せず", advice.Action);
    }

    [Fact]
    public void Analyze_RecommendsClosingTabsForHeavyBrowser()
    {
        var app = new ApplicationSnapshot("chrome", "Google Chrome", ApplicationCategory.Browser, 14, 2, 2_000_000_000, 2_500_000_000, false);

        var advice = new RecommendationEngine().Analyze(app, new ApplicationTrend(0, TimeSpan.FromMinutes(1)), NormalMemory);

        Assert.Equal(SafetyLevel.Safe, advice.Safety);
        Assert.Contains("タブ", advice.Action);
    }

    [Fact]
    public void Analyze_PrioritizesSustainedGrowth()
    {
        var app = new ApplicationSnapshot("sample", "Sample", ApplicationCategory.General, 1, 1, 900_000_000, 1_100_000_000, false);
        var trend = new ApplicationTrend(300L * 1024 * 1024, TimeSpan.FromMinutes(2));

        var advice = new RecommendationEngine().Analyze(app, trend, NormalMemory);

        Assert.Equal(100, advice.AttentionScore);
        Assert.Equal(SafetyLevel.CheckFirst, advice.Safety);
        Assert.Contains("再起動", advice.Action);
    }

    [Fact]
    public void Analyze_DoesNotCallShortSamplingNoiseSustainedGrowth()
    {
        var app = new ApplicationSnapshot("sample", "Sample", ApplicationCategory.General, 1, 1, 900_000_000, 1_100_000_000, false);
        var trend = new ApplicationTrend(300L * 1024 * 1024, TimeSpan.FromSeconds(30));

        var advice = new RecommendationEngine().Analyze(app, trend, NormalMemory);

        Assert.NotEqual(100, advice.AttentionScore);
        Assert.DoesNotContain("増え続け", advice.Reason);
    }
}
