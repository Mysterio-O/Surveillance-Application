using SurveilWin.Api.Data;
using SurveilWin.Api.Data.Entities;

namespace SurveilWin.Api.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;
    public AuditService(AppDbContext db) { _db = db; }

    public async Task LogAsync(Guid? orgId, Guid? actorId, string action, string? resourceType = null, string? resourceId = null, string? ip = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            OrganizationId = orgId,
            ActorUserId = actorId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            IpAddress = ip,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
