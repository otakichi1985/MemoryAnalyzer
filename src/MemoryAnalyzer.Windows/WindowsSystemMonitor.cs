using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MemoryAnalyzer.Core.Monitoring;

namespace MemoryAnalyzer.Windows;

public sealed class WindowsSystemMonitor : ISystemMonitor
{
    private readonly Dictionary<string, string> _productNameCache = new(StringComparer.OrdinalIgnoreCase);

    public MonitoringSnapshot Capture()
    {
        var processes = new List<ProcessObservation>();
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    processes.Add(new ProcessObservation(
                        process.Id,
                        process.ProcessName,
                        GetProductName(process),
                        GetWindowTitle(process),
                        process.WorkingSet64,
                        process.PrivateMemorySize64,
                        GetPrivateWorkingSet(process)));
                }
                catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or NotSupportedException)
                {
                    // A process can exit or become inaccessible between enumeration and reading.
                }
            }
        }

        return new MonitoringSnapshot(DateTimeOffset.Now, ReadSystemMemory(), processes);
    }

    private string GetProductName(Process process)
    {
        if (_productNameCache.TryGetValue(process.ProcessName, out var cached))
        {
            return cached;
        }

        try
        {
            var value = process.MainModule?.FileVersionInfo.ProductName?.Trim() ?? string.Empty;
            _productNameCache[process.ProcessName] = value;
            return value;
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or NotSupportedException)
        {
            return string.Empty;
        }
    }

    private static string GetWindowTitle(Process process)
    {
        try
        {
            return process.MainWindowTitle;
        }
        catch (InvalidOperationException)
        {
            return string.Empty;
        }
    }

    private static long GetPrivateWorkingSet(Process process)
    {
        try
        {
            var counters = new ProcessMemoryCountersEx2
            {
                Size = (uint)Marshal.SizeOf<ProcessMemoryCountersEx2>()
            };
            return GetProcessMemoryInfo(process.Handle, ref counters, counters.Size)
                ? checked((long)counters.PrivateWorkingSetSize.ToUInt64())
                : 0;
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or NotSupportedException or OverflowException)
        {
            return 0;
        }
    }

    private static SystemMemorySnapshot ReadSystemMemory()
    {
        var information = new PerformanceInformation
        {
            Size = (uint)Marshal.SizeOf<PerformanceInformation>()
        };

        if (!GetPerformanceInfo(ref information, information.Size))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var pageSize = checked((long)information.PageSize.ToUInt64());
        return new SystemMemorySnapshot(
            checked((long)information.PhysicalTotal.ToUInt64() * pageSize),
            checked((long)information.PhysicalAvailable.ToUInt64() * pageSize),
            checked((long)information.CommitTotal.ToUInt64() * pageSize),
            checked((long)information.CommitLimit.ToUInt64() * pageSize),
            checked((long)information.SystemCache.ToUInt64() * pageSize),
            checked((long)information.KernelPaged.ToUInt64() * pageSize),
            checked((long)information.KernelNonPaged.ToUInt64() * pageSize));
    }

    [DllImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetPerformanceInfo(ref PerformanceInformation performanceInformation, uint size);

    [DllImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessMemoryInfo(IntPtr process, ref ProcessMemoryCountersEx2 counters, uint size);

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessMemoryCountersEx2
    {
        public uint Size;
        public uint PageFaultCount;
        public UIntPtr PeakWorkingSetSize;
        public UIntPtr WorkingSetSize;
        public UIntPtr QuotaPeakPagedPoolUsage;
        public UIntPtr QuotaPagedPoolUsage;
        public UIntPtr QuotaPeakNonPagedPoolUsage;
        public UIntPtr QuotaNonPagedPoolUsage;
        public UIntPtr PagefileUsage;
        public UIntPtr PeakPagefileUsage;
        public UIntPtr PrivateUsage;
        public UIntPtr PrivateWorkingSetSize;
        public ulong SharedCommitUsage;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PerformanceInformation
    {
        public uint Size;
        public UIntPtr CommitTotal;
        public UIntPtr CommitLimit;
        public UIntPtr CommitPeak;
        public UIntPtr PhysicalTotal;
        public UIntPtr PhysicalAvailable;
        public UIntPtr SystemCache;
        public UIntPtr KernelTotal;
        public UIntPtr KernelPaged;
        public UIntPtr KernelNonPaged;
        public UIntPtr PageSize;
        public uint HandleCount;
        public uint ProcessCount;
        public uint ThreadCount;
    }
}
