namespace MemoryAnalyzer.Core.Analysis;

public sealed record MemoryComposition(
    long AvailableBytes,
    long ApplicationPrivateBytes,
    long WindowsPrivateBytes,
    long KernelBytes,
    long SharedCacheAndOtherBytes,
    long TotalBytes);
