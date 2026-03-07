namespace SurveilWin.Api.Data.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = "";
    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}
