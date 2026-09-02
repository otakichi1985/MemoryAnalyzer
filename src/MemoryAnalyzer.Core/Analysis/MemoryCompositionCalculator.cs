using MemoryAnalyzer.Core.Aggregation;
using MemoryAnalyzer.Core.Monitoring;

namespace MemoryAnalyzer.Core.Analysis;

public sealed class MemoryCompositionCalculator
{
    public MemoryComposition Calculate(
        SystemMemorySnapshot memory,
        IReadOnlyCollection<ApplicationSnapshot> applications)
    {
        var total = Math.Max(0, memory.PhysicalTotalBytes);
        var available = Math.Clamp(memory.PhysicalAvailableBytes, 0, total);
        var usedBudget = total - available;
        var applicationPrivate = Math.Min(
            usedBudget,
            applications.Where(application => !application.IsProtected).Sum(application => application.PrivateWorkingSetBytes));
        var remaining = usedBudget - applicationPrivate;
        var windowsPrivate = Math.Min(
            remaining,
            applications.Where(application => application.IsProtected).Sum(application => application.PrivateWorkingSetBytes));
        remaining -= windowsPrivate;
        var kernel = Math.Min(remaining, Math.Max(0, memory.KernelTotalBytes));
        remaining -= kernel;

        return new MemoryComposition(
            available,
            applicationPrivate,
            windowsPrivate,
            kernel,
            remaining,
            total);
    }
}
