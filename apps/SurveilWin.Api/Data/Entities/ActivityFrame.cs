namespace SurveilWin.Api.Data.Entities;

public class ActivityFrame
{
    public long Id { get; set; }
    public Guid SessionId { get; set; }
    public ActivitySession Session { get; set; } = null!;
    public Guid EmployeeId { get; set; }
    public Guid OrganizationId { get; set; }
    public DateTime CapturedAt { get; set; }
    public string ActiveApp { get; set; } = "";
    public string WindowTitle { get; set; } = "";
    public string? AppCategory { get; set; }
    public bool IsIdle { get; set; }
    public string? IdleReason { get; set; }
    public string? OcrText { get; set; }
    public string? BrowserDomain { get; set; }
    public short? MonitorIndex { get; set; }
    public int? CursorX { get; set; }
    public int? CursorY { get; set; }
    public string? ThumbnailPath { get; set; }
    public DateTime CreatedAt { get; set; }
}
