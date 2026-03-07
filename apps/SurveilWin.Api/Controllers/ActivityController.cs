using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveilWin.Api.DTOs.Activity;
using SurveilWin.Api.Extensions;
using SurveilWin.Api.Services;

namespace SurveilWin.Api.Controllers;

[ApiController]
[Route("api/activity")]
[Authorize]
public class ActivityController : ControllerBase
{
    private readonly IActivityService _activity;
    private readonly IResourceAuthorizationService _authz;

    public ActivityController(IActivityService activity, IResourceAuthorizationService authz)
    {
        _activity = activity; _authz = authz;
    }

    [HttpPost("frames")]
    public async Task<IActionResult> UploadFrames([FromBody] FrameBatchRequest req)
    {
        var employeeId = User.GetUserId();
        var orgId = User.GetOrgId();
        var count = await _activity.SaveFrameBatchAsync(employeeId, orgId, req);
        return Accepted(new { saved = count });
    }

    [HttpGet("my")]
    public async Task<IActionResult> MyActivity([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var employeeId = User.GetUserId();
        var fromDt = from ?? DateTime.UtcNow.Date;
        var toDt = to ?? DateTime.UtcNow;
        var summaries = await _activity.GetEmployeeActivityAsync(employeeId, fromDt, toDt);
        return Ok(summaries);
    }

    [HttpGet("employee/{id}")]
    [Authorize(Roles = "OrgAdmin,Manager,SuperAdmin")]
    public async Task<IActionResult> EmployeeActivity(Guid id, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var requestorId = User.GetUserId();
        if (!await _authz.CanViewEmployeeActivityAsync(requestorId, id))
            return Forbid();

        var fromDt = from ?? DateTime.UtcNow.Date;
        var toDt = to ?? DateTime.UtcNow;
        var summaries = await _activity.GetEmployeeActivityAsync(id, fromDt, toDt);
        return Ok(summaries);
    }
}
