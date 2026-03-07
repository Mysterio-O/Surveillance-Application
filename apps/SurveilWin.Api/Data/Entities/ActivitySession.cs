namespace SurveilWin.Api.Data.Entities;

public class ActivitySession
{
    public Guid Id { get; set; }
    public Guid ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;
    public Guid EmployeeId { get; set; }
    public Guid OrganizationId { get; set; }
    public string SessionKey { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int TotalFrames { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<ActivityFrame> Frames { get; set; } = new List<ActivityFrame>();
}
