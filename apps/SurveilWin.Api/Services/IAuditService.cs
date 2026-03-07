namespace SurveilWin.Api.Services;

public interface IAuditService
{
    Task LogAsync(Guid? orgId, Guid? actorId, string action, string? resourceType = null, string? resourceId = null, string? ip = null);
}
