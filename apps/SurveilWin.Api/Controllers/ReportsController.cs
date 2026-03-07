using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveilWin.Api.Data;
using SurveilWin.Api.Extensions;
using SurveilWin.Api.Services;
using SurveilWin.Api.Services.AI;

namespace SurveilWin.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IActivityService _activity;
    private readonly IResourceAuthorizationService _authz;

    public ReportsController(AppDbContext db, IActivityService activity, IResourceAuthorizationService authz)
    {
        _db = db; _activity = activity; _authz = authz;
    }

    [HttpGet("daily/{employeeId}/{date}")]
    public async Task<IActionResult> DailyReport(Guid employeeId, string date)
    {
        var requestorId = User.GetUserId();
        if (!await _authz.CanViewEmployeeActivityAsync(requestorId, employeeId))
            return Forbid();

        if (!DateOnly.TryParse(date, out var d))
            return BadRequest(new { message = "Invalid date" });

        var daily = await _activity.GetDailySummaryAsync(employeeId, d);
        if (daily == null) return NotFound();

        var shift = await _db.Shifts
            .Where(s => s.EmployeeId == employeeId && s.Date == d)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            date = d,
            employee = new { id = daily.EmployeeId, fullName = daily.Employee?.FullName },
            shift = shift == null ? null : new
            {
                startedAt = shift.StartedAt,
                endedAt = shift.EndedAt,
                actualHours = shift.ActualHours
            },
            totals = new
            {
                activeSeconds = daily.TotalActiveSeconds,
                idleSeconds = daily.TotalIdleSeconds,
                productivityScore = daily.ProductivityScore
            },
            aiNarrative = daily.AiNarrative,
            aiModelUsed = daily.AiModelUsed
        });
    }

    [HttpGet("team")]
    [Authorize(Roles = "OrgAdmin,Manager,SuperAdmin")]
    public async Task<IActionResult> TeamReport([FromQuery] string? date)
    {
        var orgId = User.GetOrgId();
        var d = DateOnly.TryParse(date, out var pd) ? pd : DateOnly.FromDateTime(DateTime.UtcNow);

        var shifts = await _db.Shifts
            .Include(s => s.Employee)
            .Where(s => s.OrganizationId == orgId && s.Date == d)
            .ToListAsync();

        return Ok(shifts.Select(s => new
        {
            employeeId = s.EmployeeId,
            employeeName = s.Employee.FullName,
            status = s.Status.ToString(),
            startedAt = s.StartedAt,
            endedAt = s.EndedAt,
            actualHours = s.ActualHours
        }));
    }

    [HttpPost("generate-ai-summary")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> GenerateAiSummary(
        [FromBody] GenerateAiSummaryRequest request,
        [FromServices] DailySummaryGenerator generator)
    {
        await generator.GenerateForDateAsync(request.Date);
        return Ok(new { message = "Summary generation started" });
    }
}

public record GenerateAiSummaryRequest(DateOnly Date);
