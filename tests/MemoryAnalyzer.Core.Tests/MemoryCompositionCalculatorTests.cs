using MemoryAnalyzer.Core.Aggregation;
using MemoryAnalyzer.Core.Analysis;
using MemoryAnalyzer.Core.Monitoring;

namespace MemoryAnalyzer.Core.Tests;

public sealed class MemoryCompositionCalculatorTests
{
    private const long Gib = 1024L * 1024 * 1024;

    [Fact]
    public void Calculate_ProducesExclusiveSegmentsThatEqualPhysicalTotal()
    {
        var memory = new SystemMemorySnapshot(32 * Gib, 8 * Gib, 30 * Gib, 48 * Gib, 4 * Gib, 1 * Gib, 1 * Gib);
        ApplicationSnapshot[] applications =
        [
            new("app", "App", ApplicationCategory.General, 1, 1, 12 * Gib, 14 * Gib, false, 10 * Gib),
            new("system", "Windows", ApplicationCategory.System, 1, 0, 3 * Gib, 3 * Gib, true, 2 * Gib)
        ];

        var result = new MemoryCompositionCalculator().Calculate(memory, applications);

        Assert.Equal(32 * Gib, result.AvailableBytes + result.ApplicationPrivateBytes
            + result.WindowsPrivateBytes + result.KernelBytes + result.SharedCacheAndOtherBytes);
        Assert.Equal(10 * Gib, result.ApplicationPrivateBytes);
        Assert.Equal(2 * Gib, result.WindowsPrivateBytes);
        Assert.Equal(2 * Gib, result.KernelBytes);
        Assert.Equal(10 * Gib, result.SharedCacheAndOtherBytes);
    }
}
