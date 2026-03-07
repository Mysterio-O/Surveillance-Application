using System.Text.Json;

namespace SurveilWin.Api.Data.Entities;

public class DailySummary
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public User Employee { get; set; } = null!;
    public Guid OrganizationId { get; set; }
    public DateOnly Date { get; set; }
    public int TotalActiveSeconds { get; set; }
    public int TotalIdleSeconds { get; set; }
    public DateTime? ShiftStart { get; set; }
    public DateTime? ShiftEnd { get; set; }
    public JsonDocument TopApps { get; set; } = JsonDocument.Parse("[]");
    public string? AiNarrative { get; set; }
    public string? AiModelUsed { get; set; }
    public DateTime? AiGeneratedAt { get; set; }
    public decimal? ProductivityScore { get; set; }
    public DateTime CreatedAt { get; set; }
}
