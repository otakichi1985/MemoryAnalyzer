using MemoryAnalyzer.Core.Monitoring;

namespace MemoryAnalyzer.Core.Aggregation;

public sealed class ApplicationAggregator
{
    private static readonly HashSet<string> ProtectedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "idle", "system", "registry", "memory compression", "secure system",
        "smss", "csrss", "wininit", "services", "lsass", "winlogon",
        "svchost", "dwm", "fontdrvhost", "msmpeng", "securityhealthservice"
    };

    private static readonly Dictionary<string, (string Display, ApplicationCategory Category)> KnownApps =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["chrome"] = ("Google Chrome", ApplicationCategory.Browser),
            ["msedge"] = ("Microsoft Edge", ApplicationCategory.Browser),
            ["firefox"] = ("Mozilla Firefox", ApplicationCategory.Browser),
            ["brave"] = ("Brave", ApplicationCategory.Browser),
            ["discord"] = ("Discord", ApplicationCategory.Communication),
            ["slack"] = ("Slack", ApplicationCategory.Communication),
            ["teams"] = ("Microsoft Teams", ApplicationCategory.Communication),
            ["chatgpt"] = ("ChatGPT", ApplicationCategory.Communication),
            ["clipstudiopaint"] = ("CLIP STUDIO PAINT", ApplicationCategory.General),
            ["pureref"] = ("PureRef", ApplicationCategory.General),
            ["code"] = ("Visual Studio Code", ApplicationCategory.Development),
            ["devenv"] = ("Visual Studio", ApplicationCategory.Development),
            ["java"] = ("Java", ApplicationCategory.Development),
            ["node"] = ("Node.js", ApplicationCategory.Development),
            ["dotnet"] = (".NET", ApplicationCategory.Development),
            ["bun"] = ("Bun", ApplicationCategory.Development),
            ["codex"] = ("Codex", ApplicationCategory.Development),
            ["opencode"] = ("OpenCode", ApplicationCategory.Development),
            ["steam"] = ("Steam", ApplicationCategory.Gaming),
            ["steamwebhelper"] = ("Steam Web Helper", ApplicationCategory.Gaming),
            ["taskbarhero"] = ("TaskBarHero", ApplicationCategory.General),
            ["mcss"] = ("MC Server Soft", ApplicationCategory.General)
        };

    public IReadOnlyList<ApplicationSnapshot> Aggregate(IEnumerable<ProcessObservation> processes)
    {
        return processes
            .GroupBy(process => Normalize(process.ProcessName), StringComparer.OrdinalIgnoreCase)
            .Select(CreateApplication)
            .OrderByDescending(application => application.WorkingSetBytes)
            .ToArray();
    }

    private static ApplicationSnapshot CreateApplication(IGrouping<string, ProcessObservation> group)
    {
        var items = group.ToArray();
        var representative = items.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.ProductName)) ?? items[0];
        var known = KnownApps.GetValueOrDefault(group.Key);
        var protectedProcess = ProtectedNames.Contains(group.Key)
            || (representative.ProductName.Contains("Windows", StringComparison.OrdinalIgnoreCase)
                && representative.ProductName.Contains("Operating System", StringComparison.OrdinalIgnoreCase));
        var displayName = known.Display
            ?? CleanProductName(representative.ProductName)
            ?? ToDisplayName(group.Key);

        return new ApplicationSnapshot(
            group.Key,
            displayName,
            protectedProcess ? ApplicationCategory.System : known.Category,
            items.Length,
            items.Count(item => !string.IsNullOrWhiteSpace(item.WindowTitle)),
            items.Sum(item => Math.Max(0, item.WorkingSetBytes)),
            items.Sum(item => Math.Max(0, item.PrivateBytes)),
            protectedProcess,
            items.Sum(item => Math.Max(0, item.PrivateWorkingSetBytes)));
    }

    private static string Normalize(string name) => name.Trim().ToLowerInvariant();

    private static string? CleanProductName(string productName)
    {
        var value = productName.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string ToDisplayName(string name) => name.Length switch
    {
        0 => "不明なアプリ",
        1 => name.ToUpperInvariant(),
        _ => char.ToUpperInvariant(name[0]) + name[1..]
    };
}
