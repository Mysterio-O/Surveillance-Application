using SurveilWin.Api.Data.Entities;
using SurveilWin.Api.DTOs.Activity;

namespace SurveilWin.Api.Services;

public interface IActivityService
{
    Task<int> SaveFrameBatchAsync(Guid employeeId, Guid orgId, FrameBatchRequest request);
    Task<IEnumerable<ActivitySummary>> GetEmployeeActivityAsync(Guid employeeId, DateTime from, DateTime to);
    Task<DailySummary?> GetDailySummaryAsync(Guid employeeId, DateOnly date);
}
