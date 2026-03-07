using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SurveilWin.Api.DTOs.Shifts;
using SurveilWin.Api.Extensions;
using SurveilWin.Api.Services;

namespace SurveilWin.Api.Controllers;

[ApiController]
[Route("api/shifts")]
[Authorize]
public class ShiftsController : ControllerBase
{
    private readonly IShiftService _shifts;
    public ShiftsController(IShiftService shifts) { _shifts = shifts; }

    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] StartShiftRequest req)
    {
        var shift = await _shifts.StartShiftAsync(User.GetUserId(), req.AgentVersion);
        return Ok(new ShiftDto
        {
            Id = shift.Id,
            EmployeeId = shift.EmployeeId,
            Date = shift.Date,
            StartedAt = shift.StartedAt,
            EndedAt = shift.EndedAt,
            ExpectedHours = shift.ExpectedHours,
            ActualHours = shift.ActualHours,
            Status = shift.Status.ToString(),
            AgentVersion = shift.AgentVersion
        });
    }

    [HttpPost("{id}/end")]
    public async Task<IActionResult> End(Guid id)
    {
        var shift = await _shifts.EndShiftAsync(id, User.GetUserId());
        if (shift == null) return NotFound(new { message = "Active shift not found" });
        return Ok(new ShiftDto
        {
            Id = shift.Id,
            EmployeeId = shift.EmployeeId,
            Date = shift.Date,
            StartedAt = shift.StartedAt,
            EndedAt = shift.EndedAt,
            ExpectedHours = shift.ExpectedHours,
            ActualHours = shift.ActualHours,
            Status = shift.Status.ToString()
        });
    }

    [HttpGet("my")]
    public async Task<IActionResult> MyShifts([FromQuery] string? from, [FromQuery] string? to)
    {
        var employeeId = User.GetUserId();
        var fromDate = DateOnly.TryParse(from, out var f) ? f : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var toDate = DateOnly.TryParse(to, out var t) ? t : DateOnly.FromDateTime(DateTime.UtcNow);
        var shifts = await _shifts.GetEmployeeShiftsAsync(employeeId, fromDate, toDate);
        return Ok(shifts.Select(s => new ShiftDto
        {
            Id = s.Id, EmployeeId = s.EmployeeId, Date = s.Date,
            StartedAt = s.StartedAt, EndedAt = s.EndedAt,
            ExpectedHours = s.ExpectedHours, ActualHours = s.ActualHours,
            Status = s.Status.ToString(), AgentVersion = s.AgentVersion
        }));
    }

    [HttpGet]
    [Authorize(Roles = "OrgAdmin,Manager,SuperAdmin")]
    public async Task<IActionResult> ListOrgShifts([FromQuery] string? from, [FromQuery] string? to)
    {
        var orgId = User.GetOrgId();
        var fromDate = DateOnly.TryParse(from, out var f) ? f : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
        var toDate = DateOnly.TryParse(to, out var t) ? t : DateOnly.FromDateTime(DateTime.UtcNow);
        var shifts = await _shifts.GetOrgShiftsAsync(orgId, fromDate, toDate);
        return Ok(shifts.Select(s => new ShiftDto
        {
            Id = s.Id, EmployeeId = s.EmployeeId,
            EmployeeName = s.Employee?.FullName ?? "",
            Date = s.Date, StartedAt = s.StartedAt, EndedAt = s.EndedAt,
            ExpectedHours = s.ExpectedHours, ActualHours = s.ActualHours,
            Status = s.Status.ToString(), AgentVersion = s.AgentVersion
        }));
    }
}
