namespace Surveil.Contracts;

/// <summary>Screen-coordinate rectangle without a System.Drawing dependency.</summary>
public record ScreenRect(int Left, int Top, int Width, int Height)
{
    public int Right  => Left + Width;
    public int Bottom => Top  + Height;
}

/// <summary>Describes a connected display monitor at the time of capture.</summary>
public record MonitorInfo(
    int        Index,
    string     DeviceName,
    ScreenRect Bounds,
    bool       IsPrimary
);

/// <summary>One captured frame plus all extracted features.</summary>
public record FrameFeature(
    DateTime  Timestamp,
    string    ActiveApp,
    string    WindowTitle,
    bool      IsIdle,
    string    OcrText,
    float[]?  Embedding,
    string?   ThumbnailPath,
    string?   MonitorDevice = null,
    int       MonitorIndex  = 0,
    int       CursorX       = 0,
    int       CursorY       = 0
);

/// <summary>Rolling-window activity summary emitted by SlidingSummarizer.</summary>
public record Summary(
    DateTime       Start,
    DateTime       End,
    string         Narrative,
    string[]       Evidence,
    double         Confidence,
    string?        SessionId   = null,
    bool           IsFullTrace = false,
    FrameFeature[]? Frames     = null
);

/// <summary>Application-level configuration loaded from appsettings.json.</summary>
public class AppConfig
{
    /// <summary>Capture frames per second (0.1 – 10).</summary>
    public double CaptureFps { get; set; } = 1.0;

    /// <summary>Tesseract language code (e.g. "eng").</summary>
    public string OcrLanguage { get; set; } = "eng";

    /// <summary>Path to the CLIP ONNX model file (relative or absolute).</summary>
    public string ModelPath { get; set; } = "models/onnx/clip-vit-b32.onnx";

    /// <summary>Save JPEG thumbnail of every captured frame.</summary>
    public bool SaveThumbnails { get; set; } = false;

    /// <summary>JPEG quality for thumbnails (1-100).</summary>
    public int ThumbnailQuality { get; set; } = 75;

    /// <summary>Delete thumbnails older than this many days (0 = never).</summary>
    public int ThumbnailRetentionDays { get; set; } = 7;

    /// <summary>Delete summary JSON files older than this many days (0 = never).</summary>
    public int SummaryRetentionDays { get; set; } = 90;

    /// <summary>Only capture these process names; empty list means capture all.</summary>
    public List<string> AllowedApps { get; set; } = new();

    /// <summary>Never capture frames when any of these process names is active.</summary>
    public List<string> DeniedApps { get; set; } = new();

    /// <summary>Embed per-frame data inside summary JSON (increases file size).</summary>
    public bool FullTraceMode { get; set; } = false;

    /// <summary>Seconds without any input before a frame is flagged as idle.</summary>
    public int IdleThresholdSeconds { get; set; } = 60;

    /// <summary>Halve capture FPS automatically while the user is idle.</summary>
    public bool AdaptiveFps { get; set; } = true;

    /// <summary>Output directory for sessions, thumbnails, and summaries.</summary>
    public string SessionsDir { get; set; } = "data/sessions";

    /// <summary>Run Tesseract OCR on each captured frame.</summary>
    public bool EnableOcr { get; set; } = true;

    /// <summary>Generate CLIP embeddings via the ONNX model.</summary>
    public bool EnableEmbeddings { get; set; } = true;

    /// <summary>URL of the SurveilWin backend API.</summary>
    public string ApiBaseUrl { get; set; } = "http://localhost:8080";
}

/// <summary>Agent configuration fetched from backend after login.</summary>
public class AgentConfig
{
    public double CaptureFps { get; set; } = 1.0;
    public bool EnableOcr { get; set; } = true;
    public bool EnableScreenshots { get; set; } = false;
    public int ScreenshotIntervalMinutes { get; set; } = 5;
    public List<string> AllowedApps { get; set; } = new();
    public List<string> DeniedApps { get; set; } = new();
    public double ExpectedShiftHours { get; set; } = 8.0;
}

/// <summary>Frame data for API batch upload.</summary>
public class FrameUploadDto
{
    public DateTime CapturedAt { get; set; }
    public string ActiveApp { get; set; } = "";
    public string WindowTitle { get; set; } = "";
    public string? AppCategory { get; set; }
    public bool IsIdle { get; set; }
    public string? IdleReason { get; set; }
    public string? OcrText { get; set; }
    public string? BrowserDomain { get; set; }
    public int? MonitorIndex { get; set; }
    public int? CursorX { get; set; }
    public int? CursorY { get; set; }
    public string? ThumbnailBase64 { get; set; }
}

/// <summary>Batch upload request sent from agent to API.</summary>
public class FrameBatchUpload
{
    public string SessionKey { get; set; } = "";
    public string ShiftId { get; set; } = "";
    public List<FrameUploadDto> Frames { get; set; } = new();
}

/// <summary>Legacy policy shim kept for backwards compatibility.</summary>
public class Policy
{
    public HashSet<string> AllowedApps  { get; init; } = new();
    public HashSet<string> DeniedApps   { get; init; } = new();
    public bool            StoreThumbnails { get; init; } = false;
}
