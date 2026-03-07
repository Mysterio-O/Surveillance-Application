using Microsoft.EntityFrameworkCore;
using SurveilWin.Api.Data;
using SurveilWin.Api.Data.Entities;
using SurveilWin.Api.DTOs.Activity;

namespace SurveilWin.Api.Services;

public class ActivityService : IActivityService
{
    private readonly AppDbContext _db;

    public ActivityService(AppDbContext db) { _db = db; }

    public async Task<int> SaveFrameBatchAsync(Guid employeeId, Guid orgId, FrameBatchRequest request)
    {
        if (!Guid.TryParse(request.ShiftId, out var shiftId))
            throw new ArgumentException("Invalid ShiftId");

        var session = await _db.ActivitySessions
            .FirstOrDefaultAsync(s => s.SessionKey == request.SessionKey);

        if (session == null)
        {
            session = new ActivitySession
            {
                Id = Guid.NewGuid(),
                ShiftId = shiftId,
                EmployeeId = employeeId,
                OrganizationId = orgId,
                SessionKey = request.SessionKey,
                StartedAt = DateTime.UtcNow,
                TotalFrames = 0,
                CreatedAt = DateTime.UtcNow
            };
            _db.ActivitySessions.Add(session);
            await _db.SaveChangesAsync();
        }

        var frames = request.Frames.Select(f => new ActivityFrame
        {
            SessionId = session.Id,
            EmployeeId = employeeId,
            OrganizationId = orgId,
            CapturedAt = f.CapturedAt,
            ActiveApp = f.ActiveApp,
            WindowTitle = f.WindowTitle,
            AppCategory = f.AppCategory,
            IsIdle = f.IsIdle,
            IdleReason = f.IdleReason,
            OcrText = f.OcrText,
            BrowserDomain = f.BrowserDomain,
            MonitorIndex = (short?)f.MonitorIndex,
            CursorX = f.CursorX,
            CursorY = f.CursorY,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        await _db.ActivityFrames.AddRangeAsync(frames);
        session.TotalFrames += frames.Count;
        await _db.SaveChangesAsync();

        return frames.Count;
    }

    public async Task<IEnumerable<ActivitySummary>> GetEmployeeActivityAsync(Guid employeeId, DateTime from, DateTime to) =>
        await _db.ActivitySummaries
            .Where(s => s.EmployeeId == employeeId && s.WindowStart >= from && s.WindowEnd <= to)
            .OrderBy(s => s.WindowStart)
            .ToListAsync();

    public async Task<DailySummary?> GetDailySummaryAsync(Guid employeeId, DateOnly date) =>
        await _db.DailySummaries
            .Include(d => d.Employee)
            .FirstOrDefaultAsync(d => d.EmployeeId == employeeId && d.Date == date);
}
