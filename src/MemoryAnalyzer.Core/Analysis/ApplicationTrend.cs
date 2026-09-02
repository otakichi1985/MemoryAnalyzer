namespace MemoryAnalyzer.Core.Analysis;

public sealed record ApplicationTrend(long ChangeBytes, TimeSpan ObservedFor)
{
    public static TimeSpan MinimumObservation { get; } = TimeSpan.FromMinutes(1);

    public bool IsGrowing => ObservedFor >= MinimumObservation && ChangeBytes >= 128L * 1024 * 1024;
}
