
namespace Surveil.Contracts;

public record FrameFeature(
    DateTime Timestamp,
    string ActiveApp,
    string WindowTitle,
    bool IsIdle,
    string OcrText,
    float[]? Embedding,
    string? ThumbnailPath
);

public record Summary(
    DateTime Start,
    DateTime End,
    string Narrative,
    string[] Evidence,
    double Confidence
);

public class Policy
{
    public HashSet<string> AllowedApps { get; init; } = new();
    public HashSet<string> DeniedApps { get; init; } = new();
    public bool StoreThumbnails { get; init; } = false;
}
