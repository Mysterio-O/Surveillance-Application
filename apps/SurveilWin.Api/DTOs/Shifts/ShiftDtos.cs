namespace SurveilWin.Api.DTOs.Shifts;

public class StartShiftRequest
{
    public string? AgentVersion { get; set; }
}

public class ShiftDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public DateOnly Date { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public decimal ExpectedHours { get; set; }
    public decimal? ActualHours { get; set; }
    public string Status { get; set; } = "";
    public string? AgentVersion { get; set; }
}
