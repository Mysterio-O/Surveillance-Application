using Microsoft.EntityFrameworkCore;
using SurveilWin.Api.Data;
using SurveilWin.Api.Data.Entities;
using System.Text.Json;

namespace SurveilWin.Api.Services.AI;

public class DailySummaryGenerator
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly LlmProviderFactory _llmFactory;
    private readonly ILogger<DailySummaryGenerator> _logger;

    public DailySummaryGenerator(
        IServiceScopeFactory scopeFactory,
        LlmProviderFactory llmFactory,
        ILogger<DailySummaryGenerator> logger)
    {
        _scopeFactory = scopeFactory;
        _llmFactory = llmFactory;
        _logger = logger;
    }

    public async Task GenerateForDateAsync(DateOnly date, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // ActivitySession.StartedAt is DateTime (UTC)
        var dayStart = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        var sessions = await db.ActivitySessions
            .Include(s => s.Shift)
            .ThenInclude(sh => sh.Employee)
            .Where(s => s.StartedAt >= dayStart && s.StartedAt < dayEnd)
            .ToListAsync(ct);

        foreach (var session in sessions)
        {
            var employee = session.Shift?.Employee;
            if (employee == null) continue;

            var existing = await db.DailySummaries
                .AnyAsync(d => d.EmployeeId == employee.Id && d.Date == date, ct);
            if (existing) continue;

            // ActivitySummary is linked by ShiftId, not SessionId
            var windows = await db.ActivitySummaries
                .Where(a => a.ShiftId == session.ShiftId)
                .OrderBy(a => a.WindowStart)
                .ToListAsync(ct);

            if (!windows.Any())
            {
                await GenerateRuleBasedAsync(db, employee, date, ct);
                continue;
            }

            var totalActive = windows.Sum(w => w.ActiveSeconds);
            var totalIdle = windows.Sum(w => w.IdleSeconds);
            var avgScore = (double)windows.Average(w => w.ProductivityScore ?? 0m);

            var appAgg = new Dictionary<string, (string category, int seconds)>();
            foreach (var w in windows)
            {
                if (w.TopApps == null) continue;
                try
                {
                    var apps = w.TopApps.Deserialize<List<AppEntry>>(
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (apps == null) continue;
                    foreach (var app in apps)
                    {
                        if (appAgg.TryGetValue(app.App, out var existing2))
                            appAgg[app.App] = (app.Category, existing2.seconds + app.Seconds);
                        else
                            appAgg[app.App] = (app.Category, app.Seconds);
                    }
                }
                catch { }
            }

            var topApps = appAgg
                .OrderByDescending(a => a.Value.seconds)
                .Take(8)
                .Select(a => new { app = a.Key, category = a.Value.category, minutes = a.Value.seconds / 60 })
                .ToList();

            string? aiNarrative = null;
            string? modelUsed = null;
            var provider = _llmFactory.GetProvider();
            if (provider != null)
            {
                var prompt = BuildPrompt(
                    employee.FirstName ?? employee.Email, date,
                    totalActive, totalIdle, avgScore,
                    topApps.Select(a => (a.app, a.category, a.minutes)));
                aiNarrative = await provider.CompleteAsync(prompt, ct);
                if (aiNarrative != null) modelUsed = provider.ProviderName;
            }

            aiNarrative ??= BuildRuleBasedNarrative(
                employee.FirstName ?? employee.Email,
                totalActive, totalIdle, avgScore,
                topApps.Select(a => (a.app, a.category, a.minutes)));

            var topAppDoc = JsonSerializer.SerializeToDocument(topApps);
            var summary = new DailySummary
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                OrganizationId = employee.OrganizationId,
                Date = date,
                TotalActiveSeconds = totalActive,
                TotalIdleSeconds = totalIdle,
                ProductivityScore = (decimal)avgScore,
                TopApps = topAppDoc,
                AiNarrative = aiNarrative,
                AiModelUsed = modelUsed,
                AiGeneratedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            };

            db.DailySummaries.Add(summary);
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Generated daily summary for {Email} on {Date}", employee.Email, date);
        }
    }

    private async Task GenerateRuleBasedAsync(
        AppDbContext db, User employee, DateOnly date, CancellationToken ct)
    {
        var narrative = $"No activity recorded for {employee.FirstName ?? employee.Email} on {date:MMMM d, yyyy}.";
        var summary = new DailySummary
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            OrganizationId = employee.OrganizationId,
            Date = date,
            TotalActiveSeconds = 0,
            TotalIdleSeconds = 0,
            ProductivityScore = 0,
            TopApps = JsonSerializer.SerializeToDocument(Array.Empty<object>()),
            AiNarrative = narrative,
            AiGeneratedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        db.DailySummaries.Add(summary);
        await db.SaveChangesAsync(ct);
    }

    private static string BuildPrompt(
        string name, DateOnly date,
        int activeSeconds, int idleSeconds, double avgScore,
        IEnumerable<(string app, string category, int minutes)> topApps)
    {
        var activeH = activeSeconds / 3600.0;
        var idleH = idleSeconds / 3600.0;
        var pct = Math.Round(avgScore * 100);
        var appList = string.Join(", ", topApps.Select(a => $"{a.app} ({a.category}, {a.minutes}m)"));

        return $"""
            Write a concise 2-3 sentence professional daily work summary for {name} on {date:MMMM d, yyyy}.
            Stats: {activeH:F1}h active, {idleH:F1}h idle, {pct}% productivity score.
            Top apps used: {appList}.
            Be factual, professional, and positive. Do not mention the employee's name in the summary. 
            Start with what they worked on (inferred from apps), then note productivity level. Max 80 words.
            """;
    }

    private static string BuildRuleBasedNarrative(
        string name, int activeSeconds, int idleSeconds, double avgScore,
        IEnumerable<(string app, string category, int minutes)> topApps)
    {
        var activeH = activeSeconds / 3600.0;
        var pct = Math.Round(avgScore * 100);
        var primary = topApps.FirstOrDefault();
        var categoryLabel = primary.category switch
        {
            "coding" => "software development",
            "browser_work" => "web-based work",
            "docs" => "document work",
            "communication" => "communication",
            "terminal" => "system administration",
            _ => "general tasks"
        };
        return $"Spent {activeH:F1}h on {categoryLabel} with a {pct}% productivity score. " +
               (primary != default ? $"Primary tool was {primary.app}." : "");
    }

    private class AppEntry
    {
        public string App { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Seconds { get; set; }
    }
}
