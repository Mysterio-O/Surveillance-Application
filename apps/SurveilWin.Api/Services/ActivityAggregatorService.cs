using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SurveilWin.Api.Data;
using SurveilWin.Api.Data.Entities;

namespace SurveilWin.Api.Services;

public class ActivityAggregatorService
{
    private readonly AppDbContext _db;
    private readonly ProductivityScorerService _scorer;

    public ActivityAggregatorService(AppDbContext db, ProductivityScorerService scorer)
    {
        _db = db;
        _scorer = scorer;
    }

    public async Task AggregateFramesForShiftAsync(Guid shiftId, CancellationToken ct = default)
    {
        var shift = await _db.Shifts.FindAsync(new object[] { shiftId }, ct);
        if (shift == null) return;

        // Get frames associated with sessions belonging to this shift
        var frames = await _db.ActivityFrames
            .Where(f => _db.ActivitySessions.Any(s => s.ShiftId == shiftId && s.Id == f.SessionId))
            .OrderBy(f => f.CapturedAt)
            .ToListAsync(ct);

        if (!frames.Any()) return;

        // Group by 5-minute windows
        var windows = frames
            .GroupBy(f => new DateTime(
                f.CapturedAt.Year, f.CapturedAt.Month, f.CapturedAt.Day,
                f.CapturedAt.Hour, (f.CapturedAt.Minute / 5) * 5, 0, DateTimeKind.Utc))
            .ToList();

        foreach (var window in windows)
        {
            var windowStart = window.Key;
            var windowEnd = windowStart.AddMinutes(5);
            var windowFrames = window.ToList();

            var existing = await _db.ActivitySummaries
                .FirstOrDefaultAsync(s =>
                    s.ShiftId == shiftId &&
                    s.WindowStart == windowStart, ct);

            if (existing != null) continue;

            var idleFrames   = windowFrames.Count(f => f.IsIdle);
            var activeFrames = windowFrames.Count(f => !f.IsIdle);
            var score        = (decimal)_scorer.Score(windowFrames);

            // Build top_apps JSON
            var appDwellTimes = windowFrames
                .GroupBy(f => f.ActiveApp)
                .Select(g =>
                {
                    var seconds = g.Count();
                    return new
                    {
                        app         = g.Key,
                        displayName = System.IO.Path.GetFileNameWithoutExtension(g.Key),
                        category    = g.First().AppCategory ?? "other",
                        seconds,
                        percent     = Math.Round(seconds * 100.0 / windowFrames.Count, 1)
                    };
                })
                .OrderByDescending(a => a.seconds)
                .Take(10)
                .ToList();

            var summary = new ActivitySummary
            {
                Id               = Guid.NewGuid(),
                ShiftId          = shiftId,
                EmployeeId       = shift.EmployeeId,
                OrganizationId   = shift.OrganizationId,
                WindowStart      = windowStart,
                WindowEnd        = windowEnd,
                TopApps          = JsonDocument.Parse(JsonSerializer.Serialize(appDwellTimes)),
                IdleSeconds      = idleFrames,
                ActiveSeconds    = activeFrames,
                WindowTitles     = JsonDocument.Parse(JsonSerializer.Serialize(
                    windowFrames.Select(f => f.WindowTitle).Distinct().Take(10).ToList())),
                ProductivityScore = score,
                CreatedAt        = DateTime.UtcNow
            };

            _db.ActivitySummaries.Add(summary);
        }

        await _db.SaveChangesAsync(ct);
    }
}
