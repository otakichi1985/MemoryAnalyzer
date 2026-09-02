namespace MemoryAnalyzer.Core.Monitoring;

public sealed record MonitoringSnapshot(
    DateTimeOffset CapturedAt,
    SystemMemorySnapshot SystemMemory,
    IReadOnlyList<ProcessObservation> Processes);
