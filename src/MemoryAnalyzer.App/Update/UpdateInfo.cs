namespace MemoryAnalyzer.App.Update;

/// <summary>GitHub Releasesから取得した最新版の情報。</summary>
public sealed class UpdateInfo
{
    public required Version Version { get; init; }
    public required string ZipUrl { get; init; }
    public string? ChecksumUrl { get; init; }
    public string ReleaseNotes { get; init; } = "";

    public bool IsNewerThan(Version current) => Version > current;
}
