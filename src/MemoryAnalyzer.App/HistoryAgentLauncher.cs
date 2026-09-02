using System.Diagnostics;
using System.IO;

namespace MemoryAnalyzer.App;

internal static class HistoryAgentLauncher
{
    public static void EnsureRunning()
    {
        if (Process.GetProcessesByName("MemoryAnalyzer.Agent").Length > 0) return;

        var agentPath = Path.Combine(AppContext.BaseDirectory, "MemoryAnalyzer.Agent.exe");
        if (!File.Exists(agentPath)) return;
        Process.Start(new ProcessStartInfo(agentPath) { UseShellExecute = true });
    }
}
