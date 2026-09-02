namespace MemoryAnalyzer.Core.Monitoring;

public sealed record SystemMemorySnapshot(
    long PhysicalTotalBytes,
    long PhysicalAvailableBytes,
    long CommitTotalBytes,
    long CommitLimitBytes,
    long SystemCacheBytes = 0,
    long KernelPagedBytes = 0,
    long KernelNonPagedBytes = 0)
{
    public double PhysicalUsedPercent => PhysicalTotalBytes == 0
        ? 0
        : (double)(PhysicalTotalBytes - PhysicalAvailableBytes) / PhysicalTotalBytes * 100;

    public double CommitUsedPercent => CommitLimitBytes == 0
        ? 0
        : (double)CommitTotalBytes / CommitLimitBytes * 100;

    public long CommitHeadroomBytes => Math.Max(0, CommitLimitBytes - CommitTotalBytes);

    public long KernelTotalBytes => KernelPagedBytes + KernelNonPagedBytes;

    public double PhysicalAvailablePercent => PhysicalTotalBytes == 0
        ? 0
        : (double)PhysicalAvailableBytes / PhysicalTotalBytes * 100;
}
