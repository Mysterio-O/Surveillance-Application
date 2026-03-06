using Surveil.Contracts;
using Surveil.Utils;

namespace Surveil.Processing;

/// <summary>
/// Maintains a rolling buffer of <see cref="FrameFeature"/> objects and fires
/// <see cref="OnSummary"/> whenever at least 30 seconds of activity data have
/// accumulated in the current 60-second window.
/// </summary>
public class SlidingSummarizer
{
    private readonly TimeSpan          _window;
    private readonly bool              _fullTrace;
    private readonly List<FrameFeature> _buffer = new();
    private readonly object            _lock    = new();

    public event Action<Summary>? OnSummary;

    public string SessionId { get; }

    // -----------------------------------------------------------------------
    // Construction
    // -----------------------------------------------------------------------

    public SlidingSummarizer(
        TimeSpan? window     = null,
        bool      fullTrace  = false,
        string?   sessionId  = null)
    {
        _window    = window ?? TimeSpan.FromSeconds(60);
        _fullTrace = fullTrace;
        SessionId  = sessionId ?? $"session_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>Adds a frame to the buffer and emits a summary when ready.</summary>
    public void Add(FrameFeature f)
    {
        lock (_lock)
        {
            _buffer.Add(f);

            // Drop frames that have fallen outside the sliding window.
            var cutoff = DateTime.UtcNow - _window;
            _buffer.RemoveAll(x => x.Timestamp < cutoff);

            var span = _buffer.Count > 1
                ? _buffer[^1].Timestamp - _buffer[0].Timestamp
                : TimeSpan.Zero;

            if (span >= TimeSpan.FromSeconds(30))
                Emit();
        }
    }

    // -----------------------------------------------------------------------
    // Implementation
    // -----------------------------------------------------------------------

    private void Emit()
    {
        if (_buffer.Count == 0) return;

        var snapshot = _buffer.ToList();
        _buffer.Clear();

        var start = snapshot.Min(x => x.Timestamp);
        var end   = snapshot.Max(x => x.Timestamp);

        // Top apps by frame count.
        var apps = snapshot
            .GroupBy(x => x.ActiveApp)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key} ({g.Count()} frames)")
            .Take(3);

        // Monitors touched (cursor-movement tracking output).
        var monitors = snapshot
            .Where(x => !string.IsNullOrWhiteSpace(x.MonitorDevice))
            .GroupBy(x => x.MonitorDevice!)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(4)
            .ToList();

        double idleRatio = snapshot.Count(x => x.IsIdle) / (double)snapshot.Count;

        var titles = snapshot
            .Select(x => x.WindowTitle)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .Take(5);

        // Confidence: penalise idle; reward more frames and distinct monitors.
        double conf = Math.Clamp(
            (1.0 - idleRatio) * (0.6 + 0.1 * Math.Min(monitors.Count, 4)),
            0.1, 1.0);

        string monitorPart = monitors.Count > 0
            ? $"; Monitors: {string.Join(", ", monitors)}"
            : string.Empty;

        string narrative =
            $"Apps: {string.Join(", ", apps)}" +
            $"; Idle {(int)(idleRatio * 100)}%" +
            monitorPart +
            $"; Titles: {string.Join(" | ", titles)}";

        var evidence = snapshot
            .Where(x => x.ThumbnailPath is not null)
            .Select(x => x.ThumbnailPath!)
            .Distinct()
            .Take(3)
            .ToArray();

        var summary = new Summary(
            Start:      start,
            End:        end,
            Narrative:  narrative,
            Evidence:   evidence,
            Confidence: conf,
            SessionId:  SessionId,
            IsFullTrace: _fullTrace,
            Frames:     _fullTrace ? snapshot.ToArray() : null
        );

        OnSummary?.Invoke(summary);
        Log.Info($"[{SessionId}] {summary.Narrative}");
    }
}
