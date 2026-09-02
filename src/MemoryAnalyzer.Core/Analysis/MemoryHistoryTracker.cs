namespace MemoryAnalyzer.Core.Analysis;

public sealed class MemoryHistoryTracker
{
    private readonly Dictionary<string, Queue<(DateTimeOffset At, long Bytes)>> _history =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly TimeSpan _window;

    public MemoryHistoryTracker(TimeSpan? window = null)
    {
        _window = window ?? TimeSpan.FromMinutes(5);
    }

    public ApplicationTrend Record(string key, DateTimeOffset capturedAt, long workingSetBytes)
    {
        if (!_history.TryGetValue(key, out var points))
        {
            points = new Queue<(DateTimeOffset At, long Bytes)>();
            _history[key] = points;
        }

        points.Enqueue((capturedAt, workingSetBytes));
        while (points.Count > 1 && capturedAt - points.Peek().At > _window)
        {
            points.Dequeue();
        }

        var first = points.Peek();
        return new ApplicationTrend(workingSetBytes - first.Bytes, capturedAt - first.At);
    }
}
