namespace MemoryAnalyzer.Core.Monitoring;

public sealed record ProcessObservation(
    int ProcessId,
    string ProcessName,
    string ProductName,
    string WindowTitle,
    long WorkingSetBytes,
    long PrivateBytes,
    long PrivateWorkingSetBytes = 0);
