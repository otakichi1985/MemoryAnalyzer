using MemoryAnalyzer.Core.Aggregation;
using MemoryAnalyzer.Core.Monitoring;

namespace MemoryAnalyzer.Core.Tests;

public sealed class ApplicationAggregatorTests
{
    [Fact]
    public void Aggregate_GroupsProcessesByNameAndSumsMemory()
    {
        var processes = new[]
        {
            new ProcessObservation(1, "chrome", "Google Chrome", "Tab A", 500, 700),
            new ProcessObservation(2, "Chrome", "Google Chrome", "", 300, 400)
        };

        var result = new ApplicationAggregator().Aggregate(processes).Single();

        Assert.Equal("Google Chrome", result.DisplayName);
        Assert.Equal(2, result.ProcessCount);
        Assert.Equal(800, result.WorkingSetBytes);
        Assert.Equal(1100, result.PrivateBytes);
        Assert.Equal(ApplicationCategory.Browser, result.Category);
    }

    [Fact]
    public void Aggregate_MarksWindowsProcessesAsProtected()
    {
        var processes = new[]
        {
            new ProcessObservation(10, "svchost", "Host Process for Windows Services", "", 100, 100)
        };

        var result = new ApplicationAggregator().Aggregate(processes).Single();

        Assert.True(result.IsProtected);
        Assert.Equal(ApplicationCategory.System, result.Category);
    }

    [Fact]
    public void Aggregate_ProtectsUnknownWindowsComponentByProductName()
    {
        var processes = new[]
        {
            new ProcessObservation(11, "futurehost", "Microsoft® Windows® Operating System", "", 100, 100)
        };

        var result = new ApplicationAggregator().Aggregate(processes).Single();

        Assert.True(result.IsProtected);
        Assert.Equal(ApplicationCategory.System, result.Category);
    }
}
