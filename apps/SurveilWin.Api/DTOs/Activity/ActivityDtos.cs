namespace SurveilWin.Api.DTOs.Activity;

public class FrameBatchRequest
{
    public string SessionKey { get; set; } = "";
    public string ShiftId { get; set; } = "";
    public List<FrameUploadDto> Frames { get; set; } = new();
}

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

public class AgentConfigResponse
{
    public double CaptureFps { get; set; } = 1.0;
    public bool EnableOcr { get; set; } = true;
    public bool EnableScreenshots { get; set; } = false;
    public int ScreenshotIntervalMinutes { get; set; } = 5;
    public List<string> AllowedApps { get; set; } = new();
    public List<string> DeniedApps { get; set; } = new();
    public double ExpectedShiftHours { get; set; } = 8.0;
}
