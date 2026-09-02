using System.Text.Json;

namespace MemoryAnalyzer.Core.History;

public sealed class MemoryHistoryStore
{
    private readonly string _path;
    private readonly TimeSpan _sampleInterval;
    private readonly TimeSpan _retention;
    private readonly object _gate = new();
    private DateTimeOffset? _lastRecordedAt;

    public MemoryHistoryStore(
        string path,
        TimeSpan? sampleInterval = null,
        TimeSpan? retention = null)
    {
        _path = path;
        _sampleInterval = sampleInterval ?? TimeSpan.FromMinutes(1);
        _retention = retention ?? TimeSpan.FromDays(7);
        _lastRecordedAt = ReadNewestTimestamp();
        Compact(DateTimeOffset.Now);
    }

    public bool TryRecord(MemoryHistorySnapshot snapshot)
    {
        lock (_gate)
        {
            if (_lastRecordedAt is not null && snapshot.CapturedAt - _lastRecordedAt < _sampleInterval)
            {
                return false;
            }

            try
            {
                var directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
                File.AppendAllText(_path, JsonSerializer.Serialize(snapshot) + Environment.NewLine);
                _lastRecordedAt = snapshot.CapturedAt;
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }

    public IReadOnlyList<ApplicationMemoryHistorySummary> ReadSummaries(DateTimeOffset now)
    {
        lock (_gate)
        {
            var snapshots = ReadValidSnapshots(now - _retention).ToArray();
            return snapshots
                .SelectMany(snapshot => snapshot.Applications.Select(application => new { snapshot.CapturedAt, Application = application }))
                .GroupBy(item => item.Application.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var ordered = group.OrderBy(item => item.CapturedAt).ToArray();
                    var peak = ordered.MaxBy(item => item.Application.WorkingSetBytes)!;
                    return new ApplicationMemoryHistorySummary(
                        group.Key,
                        ordered[^1].Application.DisplayName,
                        ordered[^1].Application.WorkingSetBytes,
                        (long)ordered.Average(item => item.Application.WorkingSetBytes),
                        peak.Application.WorkingSetBytes,
                        peak.CapturedAt,
                        ordered[0].CapturedAt,
                        ordered[^1].CapturedAt,
                        ordered.Length);
                })
                .OrderByDescending(summary => summary.PeakWorkingSetBytes)
                .ToArray();
        }
    }

    public MemoryPressureHistorySummary ReadPressureSummary(DateTimeOffset now)
    {
        lock (_gate)
        {
            var samples = ReadValidSnapshots(now - _retention)
                .Where(snapshot => snapshot.PhysicalTotalBytes > 0 && snapshot.CommitLimitBytes > 0)
                .Select(snapshot => new
                {
                    snapshot.CapturedAt,
                    AvailablePercent = (double)snapshot.PhysicalAvailableBytes / snapshot.PhysicalTotalBytes * 100,
                    CommitPercent = (double)snapshot.CommitTotalBytes / snapshot.CommitLimitBytes * 100
                })
                .ToArray();
            if (samples.Length == 0) return new MemoryPressureHistorySummary(0, 100, 0, 0, null, 0);

            var critical = samples.Where(sample => sample.CommitPercent >= 97
                || (sample.CommitPercent >= 95 && sample.AvailablePercent <= 3.2)).ToArray();
            return new MemoryPressureHistorySummary(
                samples.Max(sample => sample.CommitPercent),
                samples.Min(sample => sample.AvailablePercent),
                critical.Length,
                samples.Count(sample => sample.CommitPercent >= 90 && sample.AvailablePercent < 10),
                critical.LastOrDefault()?.CapturedAt,
                samples.Length);
        }
    }

    private DateTimeOffset? ReadNewestTimestamp()
    {
        try
        {
            return ReadValidSnapshots(DateTimeOffset.MinValue).LastOrDefault()?.CapturedAt;
        }
        catch
        {
            return null;
        }
    }

    private IEnumerable<MemoryHistorySnapshot> ReadValidSnapshots(DateTimeOffset cutoff)
    {
        if (!File.Exists(_path)) yield break;

        foreach (var line in File.ReadLines(_path))
        {
            MemoryHistorySnapshot? snapshot;
            try
            {
                snapshot = JsonSerializer.Deserialize<MemoryHistorySnapshot>(line);
            }
            catch (JsonException)
            {
                continue;
            }

            if (snapshot is not null && snapshot.CapturedAt >= cutoff)
            {
                yield return snapshot;
            }
        }
    }

    private void Compact(DateTimeOffset now)
    {
        lock (_gate)
        {
            try
            {
                if (!File.Exists(_path)) return;
                var retained = ReadValidSnapshots(now - _retention).ToArray();
                var temporaryPath = _path + ".tmp";
                File.WriteAllLines(temporaryPath, retained.Select(snapshot => JsonSerializer.Serialize(snapshot)));
                File.Move(temporaryPath, _path, true);
                _lastRecordedAt = retained.LastOrDefault()?.CapturedAt;
            }
            catch (IOException)
            {
                // History is optional; monitoring must continue even if cleanup fails.
            }
            catch (UnauthorizedAccessException)
            {
                // History is optional; monitoring must continue even if cleanup fails.
            }
        }
    }
}
