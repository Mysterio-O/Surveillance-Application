using System.Security.Claims;

namespace SurveilWin.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static Guid GetOrgId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue("org_id")!);

    public static string GetRole(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role) ?? "";
}
