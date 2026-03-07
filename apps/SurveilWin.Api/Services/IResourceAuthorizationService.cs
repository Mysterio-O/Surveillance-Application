namespace SurveilWin.Api.Services;

public interface IResourceAuthorizationService
{
    Task<bool> CanViewEmployeeActivityAsync(Guid requestorId, Guid targetEmployeeId);
    Task<bool> CanManageUserAsync(Guid requestorId, Guid targetUserId);
    Task<bool> InSameOrgAsync(Guid requestorId, Guid targetId);
}
