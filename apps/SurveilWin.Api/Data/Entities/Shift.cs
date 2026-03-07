namespace SurveilWin.Api.Data.Entities;

public enum ShiftStatus { Active, Completed, AutoClosed }

public class Shift
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public User Employee { get; set; } = null!;
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public DateOnly Date { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public decimal ExpectedHours { get; set; } = 8.0m;
    public decimal? ActualHours { get; set; }
    public ShiftStatus Status { get; set; } = ShiftStatus.Active;
    public string? AgentVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<ActivitySession> Sessions { get; set; } = new List<ActivitySession>();
}
