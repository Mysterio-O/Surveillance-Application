using System.Text.Json;

namespace SurveilWin.Api.Data.Entities;

public class OrgPolicy
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public decimal CaptureFps { get; set; } = 1.0m;
    public bool EnableOcr { get; set; } = true;
    public bool EnableScreenshots { get; set; } = false;
    public int ScreenshotIntervalMinutes { get; set; } = 5;
    public int ScreenshotRetentionDays { get; set; } = 7;
    public int SummaryRetentionDays { get; set; } = 90;
    public JsonDocument AllowedApps { get; set; } = JsonDocument.Parse("[]");
    public JsonDocument DeniedApps { get; set; } = JsonDocument.Parse("[]");
    public decimal ExpectedShiftHours { get; set; } = 8.0m;
    public int AutoCloseShiftAfterHours { get; set; } = 12;
    public bool EnableAiSummaries { get; set; } = true;
    public string AiProvider { get; set; } = "ollama";
    public DateTime UpdatedAt { get; set; }
}
