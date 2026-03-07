using SurveilWin.Api.Data.Entities;

namespace SurveilWin.Api.Services;

public interface IShiftService
{
    Task<Shift> StartShiftAsync(Guid employeeId, string? agentVersion);
    Task<Shift?> EndShiftAsync(Guid shiftId, Guid employeeId);
    Task AutoCloseStaleShiftsAsync();
    Task<Shift?> GetActiveShiftAsync(Guid employeeId);
    Task<IEnumerable<Shift>> GetEmployeeShiftsAsync(Guid employeeId, DateOnly from, DateOnly to);
    Task<IEnumerable<Shift>> GetOrgShiftsAsync(Guid orgId, DateOnly from, DateOnly to);
}
