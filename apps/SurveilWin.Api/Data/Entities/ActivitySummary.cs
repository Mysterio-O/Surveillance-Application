using System.Text.Json;

namespace SurveilWin.Api.Data.Entities;

public class ActivitySummary
{
    public Guid Id { get; set; }
    public Guid ShiftId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid OrganizationId { get; set; }
    public DateTime WindowStart { get; set; }
    public DateTime WindowEnd { get; set; }
    public JsonDocument TopApps { get; set; } = JsonDocument.Parse("[]");
    public int IdleSeconds { get; set; }
    public int ActiveSeconds { get; set; }
    public JsonDocument WindowTitles { get; set; } = JsonDocument.Parse("[]");
    public decimal? ProductivityScore { get; set; }
    public DateTime CreatedAt { get; set; }
}
