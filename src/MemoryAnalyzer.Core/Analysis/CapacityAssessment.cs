namespace MemoryAnalyzer.Core.Analysis;

public enum CapacityStatus
{
    Comfortable,
    Observe,
    CriticalAction,
    AppActionFirst,
    ConsiderUpgrade
}

public sealed record CapacityAssessment(
    CapacityStatus Status,
    string Title,
    string Description);
