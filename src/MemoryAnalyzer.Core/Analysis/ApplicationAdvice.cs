namespace MemoryAnalyzer.Core.Analysis;

public sealed record ApplicationAdvice(
    string Reason,
    string Action,
    string Effect,
    string SideEffect,
    SafetyLevel Safety,
    int AttentionScore);
