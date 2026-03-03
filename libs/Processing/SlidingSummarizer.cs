
using Surveil.Contracts;
using Surveil.Utils;

namespace Surveil.Processing;

public class SlidingSummarizer
{
    private readonly TimeSpan _window = TimeSpan.FromSeconds(60);
    private readonly List<FrameFeature> _buffer = new();

    public event Action<Summary>? OnSummary;

    public void Add(FrameFeature f)
    {
        _buffer.Add(f);
        var cutoff = DateTime.UtcNow - _window;
        _buffer.RemoveAll(x => x.Timestamp < cutoff);

        var span = _buffer.Any() ? (_buffer.Max(x => x.Timestamp) - _buffer.Min(x => x.Timestamp)) : TimeSpan.Zero;
        if (span >= TimeSpan.FromSeconds(30)) // emit every ~30s of content
        {
            Emit();
        }
    }

    private void Emit()
    {
        if (!_buffer.Any()) return;
        var start = _buffer.Min(x => x.Timestamp);
        var end = _buffer.Max(x => x.Timestamp);

        var apps = _buffer.GroupBy(x => x.ActiveApp)
                           .OrderByDescending(g => g.Count())
                           .Select(g => $"{g.Key} ({g.Count()} frames)")
                           .Take(3);

        var idleRatio = _buffer.Count(x => x.IsIdle) / (double)_buffer.Count;
        var titles = _buffer.Select(x => x.WindowTitle).Where(s => !string.IsNullOrWhiteSpace(s)).Take(5);

        // naive confidence heuristic
        double conf = Math.Max(0.4, 1.0 - idleRatio);

        string narrative = $"Apps: {string.Join(", ", apps)}; Idle {(int)(idleRatio*100)}%; Top titles: {string.Join(" | ", titles)}";
        var evidence = _buffer.Where(x => x.ThumbnailPath != null)
                              .Select(x => x.ThumbnailPath!)
                              .Distinct()
                              .Take(3)
                              .ToArray();

        OnSummary?.Invoke(new Summary(start, end, narrative, evidence, conf));
        _buffer.Clear();

        Log.Info($"Summary: {narrative}");
    }
}
