using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

namespace MemoryAnalyzer.App.Update;

/// <summary>
/// GitHub Releasesから最新版を探し、ダウンロードして差し替える更新処理。
/// 公開リポジトリのReleasesを配布元に使う。確認・導入の判断は呼び出し側が行う。
/// </summary>
public sealed class UpdateService : IDisposable
{
    private const string RepoOwner = "otakichi1985";
    private const string RepoName = "MemoryAnalyzer";

    private readonly HttpClient _http = new();
    private readonly Version _currentVersion;

    public UpdateService()
    {
        var version = typeof(UpdateService).Assembly.GetName().Version;
        _currentVersion = version is null || version.Major == 0
            ? new Version(1, 0, 0)
            : new Version(version.Major, version.Minor, version.Build);
    }

    public Version CurrentVersion => _currentVersion;

    /// <summary>最新の公開版を確認し、見つからなければ null を返す。</summary>
    public async Task<UpdateInfo?> CheckForUpdatesAsync(CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest");
            request.Headers.UserAgent.ParseAdd(RepoName);
            request.Headers.Accept.ParseAdd("application/vnd.github+json");

            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return null;

            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var root = document.RootElement;

            var version = ParseVersion(root.TryGetProperty("tag_name", out var tag) ? tag.GetString() : null);
            if (version is null) return null;

            string? zipUrl = null;
            string? checksumUrl = null;
            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.TryGetProperty("name", out var n) ? n.GetString() : null;
                    var url = asset.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;
                    if (string.IsNullOrEmpty(url)) continue;
                    if (name is not null && name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) && zipUrl is null)
                        zipUrl = url;
                    else if (name is not null && name.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase))
                        checksumUrl = url;
                }
            }
            if (zipUrl is null) return null;

            var notes = root.TryGetProperty("body", out var body) ? body.GetString() : null;
            return new UpdateInfo
            {
                Version = version,
                ZipUrl = zipUrl,
                ChecksumUrl = checksumUrl,
                ReleaseNotes = notes ?? ""
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>配布物のzipをダウンロードして指定パスへ保存する。</summary>
    public async Task DownloadAsync(UpdateInfo info, string zipPath, IProgress<int>? progress, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, info.ZipUrl);
        request.Headers.UserAgent.ParseAdd(RepoName);
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? 0;
        await using var source = await response.Content.ReadAsStreamAsync(ct);
        await using var target = File.Create(zipPath);
        var buffer = new byte[81920];
        long read = 0;
        int chunk;
        while ((chunk = await source.ReadAsync(buffer, ct)) > 0)
        {
            await target.WriteAsync(buffer.AsMemory(0, chunk), ct);
            read += chunk;
            if (total > 0) progress?.Report((int)(read * 100 / total));
        }
    }

    /// <summary>配布物が改ざん・破損していないかを確認する。チェックサム未公開なら通す。</summary>
    public async Task VerifyChecksumAsync(UpdateInfo info, string zipPath, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(info.ChecksumUrl)) return;

        using var request = new HttpRequestMessage(HttpMethod.Get, info.ChecksumUrl);
        request.Headers.UserAgent.ParseAdd(RepoName);
        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var expected = (await response.Content.ReadAsStringAsync(ct)).Trim();

        await using var stream = File.OpenRead(zipPath);
        var hash = await SHA256.HashDataAsync(stream, ct);
        var actual = Convert.ToHexString(hash).ToLowerInvariant();

        if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("ダウンロードしたファイルの内容が正しくありません。");
    }

    /// <summary>zipを展開する（既存ファイルは上書き）。</summary>
    public void ExtractToDirectory(string zipPath, string destDir)
        => ZipFile.ExtractToDirectory(zipPath, destDir, overwriteFiles: true);

    /// <summary>
    /// アプリを閉じて最新版へ差し替え、再起動する補助スクリプトを実行する。
    /// 実行中のexeは上書きできないため、一時スクリプトが終了・置換・起動を行う。
    /// </summary>
    public void StartUpdate(string stageDir, string targetDir, string appExe)
    {
        var scriptPath = Path.Combine(Path.GetTempPath(), $"MemoryAnalyzerUpdate-{Guid.NewGuid():N}.cmd");
        var lines = new[]
        {
            "@echo off",
            "timeout /t 3 /nobreak >nul",
            "taskkill /IM MemoryAnalyzer.Agent.exe /F >nul 2>&1",
            $"taskkill /IM {appExe} /F >nul 2>&1",
            $"xcopy /y /e /q \"{stageDir}\\*\" \"{targetDir}\\\" >nul",
            $"rmdir /s /q \"{stageDir}\"",
            $"start \"\" \"{targetDir}\\{appExe}\"",
            "(goto) 2>nul & del \"%~f0\""
        };
        File.WriteAllLines(scriptPath, lines);

        var psi = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("/c");
        psi.ArgumentList.Add(scriptPath);
        Process.Start(psi);
    }

    private static Version? ParseVersion(string? tag)
    {
        if (string.IsNullOrEmpty(tag)) return null;
        var candidate = tag.TrimStart('v', 'V');
        return Version.TryParse(candidate, out var version) ? version : null;
    }

    public void Dispose() => _http.Dispose();
}
