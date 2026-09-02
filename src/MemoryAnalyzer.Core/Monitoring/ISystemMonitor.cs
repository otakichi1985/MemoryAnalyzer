namespace MemoryAnalyzer.Core.Monitoring;

public interface ISystemMonitor
{
    MonitoringSnapshot Capture();
}
