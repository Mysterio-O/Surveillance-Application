using Microsoft.EntityFrameworkCore;
using SurveilWin.Api.Data;
using SurveilWin.Api.Data.Entities;

namespace SurveilWin.Api.Services;

public class ShiftService : IShiftService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ShiftService> _logger;

    public ShiftService(AppDbContext db, ILogger<ShiftService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Shift> StartShiftAsync(Guid employeeId, string? agentVersion)
    {
        var existing = await GetActiveShiftAsync(employeeId);
        if (existing != null) return existing;

        var user = await _db.Users.FindAsync(employeeId)
            ?? throw new InvalidOperationException("User not found");

        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            OrganizationId = user.OrganizationId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            StartedAt = DateTime.UtcNow,
            Status = ShiftStatus.Active,
            AgentVersion = agentVersion,
            CreatedAt = DateTime.UtcNow
        };

        _db.Shifts.Add(shift);
        await _db.SaveChangesAsync();
        return shift;
    }

    public async Task<Shift?> EndShiftAsync(Guid shiftId, Guid employeeId)
    {
        var shift = await _db.Shifts.FirstOrDefaultAsync(
            s => s.Id == shiftId && s.EmployeeId == employeeId && s.Status == ShiftStatus.Active);

        if (shift == null) return null;

        shift.EndedAt = DateTime.UtcNow;
        shift.Status = ShiftStatus.Completed;
        shift.ActualHours = (decimal)(shift.EndedAt.Value - shift.StartedAt).TotalHours;
        await _db.SaveChangesAsync();
        return shift;
    }

    public async Task AutoCloseStaleShiftsAsync()
    {
        var staleShifts = await _db.Shifts
            .Include(s => s.Employee)
            .ThenInclude(u => u.Organization)
            .ThenInclude(o => o!.Policy)
            .Where(s => s.Status == ShiftStatus.Active)
            .ToListAsync();

        foreach (var shift in staleShifts)
        {
            var hours = shift.Employee.Organization?.Policy?.AutoCloseShiftAfterHours ?? 12;
            if ((DateTime.UtcNow - shift.StartedAt).TotalHours >= hours)
            {
                shift.EndedAt = DateTime.UtcNow;
                shift.Status = ShiftStatus.AutoClosed;
                shift.ActualHours = (decimal)(shift.EndedAt.Value - shift.StartedAt).TotalHours;
                _logger.LogInformation("Auto-closed shift {ShiftId} for employee {EmployeeId}", shift.Id, shift.EmployeeId);
            }
        }

        await _db.SaveChangesAsync();
    }

    public Task<Shift?> GetActiveShiftAsync(Guid employeeId) =>
        _db.Shifts.FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.Status == ShiftStatus.Active);

    public async Task<IEnumerable<Shift>> GetEmployeeShiftsAsync(Guid employeeId, DateOnly from, DateOnly to) =>
        await _db.Shifts
            .Where(s => s.EmployeeId == employeeId && s.Date >= from && s.Date <= to)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync();

    public async Task<IEnumerable<Shift>> GetOrgShiftsAsync(Guid orgId, DateOnly from, DateOnly to) =>
        await _db.Shifts
            .Include(s => s.Employee)
            .Where(s => s.OrganizationId == orgId && s.Date >= from && s.Date <= to)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync();
}
