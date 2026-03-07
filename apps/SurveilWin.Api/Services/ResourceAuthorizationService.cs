using Microsoft.EntityFrameworkCore;
using SurveilWin.Api.Data;
using SurveilWin.Api.Data.Entities;

namespace SurveilWin.Api.Services;

public class ResourceAuthorizationService : IResourceAuthorizationService
{
    private readonly AppDbContext _db;
    public ResourceAuthorizationService(AppDbContext db) { _db = db; }

    public async Task<bool> CanViewEmployeeActivityAsync(Guid requestorId, Guid targetEmployeeId)
    {
        if (requestorId == targetEmployeeId) return true;

        var requestor = await _db.Users.FindAsync(requestorId);
        if (requestor == null) return false;

        if (requestor.Role == UserRole.SuperAdmin) return true;

        var target = await _db.Users.FindAsync(targetEmployeeId);
        if (target == null || target.OrganizationId != requestor.OrganizationId) return false;

        if (requestor.Role == UserRole.OrgAdmin) return true;

        if (requestor.Role == UserRole.Manager)
        {
            return await _db.ManagerAssignments
                .AnyAsync(ma => ma.ManagerId == requestorId && ma.EmployeeId == targetEmployeeId);
        }

        return false;
    }

    public async Task<bool> CanManageUserAsync(Guid requestorId, Guid targetUserId)
    {
        var requestor = await _db.Users.FindAsync(requestorId);
        if (requestor == null) return false;
        if (requestor.Role == UserRole.SuperAdmin) return true;

        var target = await _db.Users.FindAsync(targetUserId);
        if (target == null || target.OrganizationId != requestor.OrganizationId) return false;

        return requestor.Role == UserRole.OrgAdmin;
    }

    public async Task<bool> InSameOrgAsync(Guid requestorId, Guid targetId)
    {
        var requestor = await _db.Users.FindAsync(requestorId);
        var target = await _db.Users.FindAsync(targetId);
        if (requestor == null || target == null) return false;
        return requestor.OrganizationId == target.OrganizationId;
    }
}
