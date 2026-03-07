# Phase 4 — Improved Activity Tracking Accuracy
## SurveilWin Production Platform

---

## Overview

This document instructs an AI agent to implement **improved activity tracking** that gives accurate insight into what an employee is doing — not just raw app names, but meaningful categories, productivity scores, and structured time breakdowns.

**Goal:** Transform raw per-second frame data into meaningful signals: "Employee spent 4.5 hours coding in VS Code, 1.2 hours on Chrome (work domains), 0.5 hours in Slack, 45 minutes idle."

---

## Prerequisites

- Phase 1 complete: Backend API accepting `ActivityFrame` records
- Phase 3 complete: Agent uploading frames to backend API

---

## Part A: App Category Classification

### Create `apps/Agent.Win/Services/AppCategoryService.cs`

Classify each captured app into a category **locally on the agent** (before upload):

```csharp
public static class AppCategoryService
{
    public static string Classify(string processName, string windowTitle)
    {
        // Returns one of: coding, browser, docs, communication, media, system, idle, other
    }
}
```

### Category Definitions

| Category | Process Names | Window Title Keywords |
|----------|-------------|----------------------|
| `coding` | devenv.exe, code.exe, rider64.exe, idea64.exe, pycharm64.exe, webstorm64.exe, notepad++.exe, vim.exe, neovide.exe, sublime_text.exe | "Visual Studio", "VS Code", "IntelliJ", "PyCharm" |
| `browser` | chrome.exe, firefox.exe, msedge.exe, opera.exe, brave.exe, vivaldi.exe | (any) |
| `docs` | WINWORD.EXE, EXCEL.EXE, POWERPNT.EXE, soffice.exe, notion.exe, obsidian.exe, onenote.exe | "Word", "Excel", "PowerPoint", "Notion", "Obsidian" |
| `communication` | slack.exe, teams.exe, discord.exe, zoom.exe, msteams.exe, webex.exe, skype.exe, telegram.exe, signal.exe, whatsapp.exe | "Slack", "Teams", "Zoom", "Discord" |
| `terminal` | WindowsTerminal.exe, cmd.exe, powershell.exe, wsl.exe, ubuntu.exe, gitbash.exe | "Terminal", "Command Prompt", "PowerShell" |
| `media` | vlc.exe, spotify.exe, wmplayer.exe, Photos.exe, mspaint.exe | (any) |
| `system` | explorer.exe, taskmgr.exe, control.exe, regedit.exe, mmc.exe | "File Explorer", "Task Manager" |
| `idle` | (any) | when `IsIdle = true` |
| `other` | anything not matched above | |

**Classification logic:**
1. If `IsIdle = true` → return `"idle"` immediately
2. Normalize processName to lowercase, strip path
3. Check process name against each category's process list (exact match or contains)
4. If no process match, check window title keywords (case-insensitive substring)
5. Default to `"other"`

### Browser Work Detection

For `browser` category, also extract the **domain** from the window title for better insight:

```csharp
public static string? ExtractBrowserDomain(string windowTitle)
{
    // Most browsers append tab title + " - Browser Name"
    // Window title examples:
    //   "GitHub - chrome"        → "github.com" context
    //   "Jira Board - Microsoft Edge" → "atlassian.net" context
    //   "YouTube - Firefox"      → "youtube.com" (non-work)

    // Strategy: extract significant keywords before " - " separator
    // Store as window_title substring (full domain extraction is not reliable from title alone)
}
```

**Work vs non-work browser heuristic (in backend classification):**
Store these patterns in `OrgPolicy.WorkDomainKeywords`:
```json
["github", "gitlab", "jira", "confluence", "notion", "figma", "linear", "trello",
 "google docs", "google sheets", "google slides", "stackoverflow", "docs.microsoft"]
```
Frame `app_category` becomes `browser_work` if window title contains any work keyword, `browser_other` otherwise.

---

## Part B: Productivity Scoring

### Add to Backend: `Services/ProductivityScorerService.cs`

Calculate a **productivity score (0.0–1.0)** for each 5-minute activity window:

```csharp
public class ProductivityScorerService
{
    public double Score(IEnumerable<ActivityFrame> frames, OrgPolicy policy)
    {
        var frameList = frames.ToList();
        if (frameList.Count == 0) return 0.0;

        double productiveSeconds = 0;
        double totalSeconds = frameList.Count; // each frame ≈ 1 second at 1 FPS

        foreach (var frame in frameList)
        {
            productiveSeconds += frame.AppCategory switch
            {
                "coding"        => 1.0,
                "docs"          => 1.0,
                "terminal"      => 1.0,
                "browser_work"  => 0.9,
                "communication" => 0.8,
                "browser"       => 0.5,   // unknown domain
                "system"        => 0.3,
                "media"         => 0.1,
                "idle"          => 0.0,
                _               => 0.4
            };
        }

        return Math.Round(productiveSeconds / totalSeconds, 2);
    }
}
```

**Store productivity score in:**
- `activity_summaries.productivity_score` (per 5-minute window)
- `daily_summaries.productivity_score` (average over full day)

---

## Part C: App Dwell Time Tracking

### Backend Service: `Services/ActivityAggregatorService.cs`

After frames are uploaded, aggregate them into **5-minute activity summary windows**:

```csharp
public async Task AggregateFramesForShiftAsync(Guid shiftId)
{
    // 1. Get all frames for the shift
    // 2. Group into 5-minute buckets (by floor(capturedAt / 5min))
    // 3. For each bucket:
    //    a. Calculate total active/idle seconds
    //    b. Calculate time per app (group by ActiveApp)
    //    c. Calculate time per category
    //    d. Compute productivity score
    //    e. Get distinct window titles
    //    f. Upsert into activity_summaries table
}
```

**App dwell time JSON structure** (stored in `activity_summaries.top_apps`):
```json
[
  { "app": "code.exe", "display_name": "VS Code", "category": "coding", "seconds": 240, "percent": 80.0 },
  { "app": "chrome.exe", "display_name": "Chrome", "category": "browser_work", "seconds": 45, "percent": 15.0 },
  { "app": "slack.exe", "display_name": "Slack", "category": "communication", "seconds": 15, "percent": 5.0 }
]
```

**Trigger aggregation:**
- Run after each frame batch upload (async background task)
- Or run on a scheduled basis (every 5 minutes via hosted service)

---

## Part D: URL Domain Extraction

### Better Browser Tracking via OCR

When `EnableOcr = true`, use the OCR text from browser frames to extract URLs:

```csharp
public static string? ExtractUrlFromOcrText(string ocrText)
{
    // Look for URL patterns: https?://[^\s]+
    // Extract domain: new Uri(url).Host
    // Return domain or null
    var urlRegex = new Regex(@"https?://([^\s/]+)");
    var match = urlRegex.Match(ocrText);
    return match.Success ? match.Groups[1].Value : null;
}
```

Store extracted domain in a new column `browser_domain VARCHAR(255)` in `activity_frames`.

This gives much richer insight: "Employee spent 2 hours on github.com, 45 minutes on stackoverflow.com" instead of just "Chrome".

---

## Part E: Idle Detection Improvements

### Enhanced Idle Detection in `ActivityService.cs`

Current: only uses `GetLastInputInfo` (keyboard/mouse).

**Improve to also detect:**

```csharp
public enum IdleReason { NotIdle, NoInput, SameCursorPosition, SameWindowTitle, ScreensaverActive }

public IdleReason GetIdleReason(TimeSpan threshold, string lastWindowTitle, System.Drawing.Point lastCursor)
{
    // 1. Check GetLastInputInfo → NoInput
    // 2. Check if cursor position unchanged for > threshold → SameCursorPosition
    // 3. Check if window title unchanged for > 5 minutes (same content visible) → SameWindowTitle
    // 4. Check GetScreenSaverRunning → ScreensaverActive
    return IdleReason.NotIdle;
}
```

Add `idle_reason` column to `activity_frames`:
```sql
ALTER TABLE activity_frames ADD COLUMN idle_reason VARCHAR(30);
```

---

## Part F: OCR Keyword Extraction

### Create `libs/Processing/KeywordExtractor.cs`

Extract meaningful keywords from OCR text to understand what the employee is working on:

```csharp
public static class KeywordExtractor
{
    public static IEnumerable<string> Extract(string ocrText)
    {
        // 1. Remove common noise (single chars, numbers, punctuation)
        // 2. Split on whitespace, filter by length (3–30 chars)
        // 3. Remove English stopwords (the, a, is, etc.)
        // 4. Extract JIRA-style ticket IDs: [A-Z]+-[0-9]+ (e.g., PROJ-123)
        // 5. Extract file paths: C:\..., /usr/..., ./src/...
        // 6. Extract git branch-like patterns: feature/..., hotfix/...
        // 7. Return top 10 tokens by frequency
    }

    public static IEnumerable<string> ExtractJiraTickets(string ocrText)
    {
        var jiraRegex = new Regex(@"\b[A-Z]{2,10}-[0-9]{1,6}\b");
        return jiraRegex.Matches(ocrText).Select(m => m.Value).Distinct();
    }
}
```

Store extracted keywords in the 5-minute activity summary:
```json
{
  "keywords": ["PROJ-456", "authentication", "middleware", "feature/login"],
  "jira_tickets": ["PROJ-456"]
}
```

---

## Part G: Configurable Screenshot Evidence

### Screenshot Capture Strategy (in Agent)

When screenshots are enabled, capture with smart sampling:

```csharp
public class ScreenshotScheduler
{
    private DateTime _lastScreenshot = DateTime.MinValue;
    private string? _lastCategory;

    public bool ShouldCapture(string category, int intervalMinutes)
    {
        // Always capture on category change (e.g., coding → communication)
        if (category != _lastCategory)
        {
            _lastCategory = category;
            _lastScreenshot = DateTime.UtcNow;
            return true;
        }

        // Capture at configured interval
        return DateTime.UtcNow - _lastScreenshot >= TimeSpan.FromMinutes(intervalMinutes);
    }
}
```

This captures ~1 screenshot per 5 minutes (configurable) but also captures context switches, providing better evidence than time-based sampling alone.

---

## Updated `FrameUploadDto`

Extend the DTO (in `libs/Contracts/Contracts.cs`) to include classification data:

```csharp
public class FrameUploadDto
{
    public DateTime CapturedAt { get; set; }
    public string ActiveApp { get; set; } = "";
    public string WindowTitle { get; set; } = "";
    public string? AppCategory { get; set; }         // classified by agent
    public bool IsIdle { get; set; }
    public string? IdleReason { get; set; }           // new
    public string? OcrText { get; set; }
    public string? BrowserDomain { get; set; }        // new: extracted URL domain
    public int? MonitorIndex { get; set; }
    public int? CursorX { get; set; }
    public int? CursorY { get; set; }
    public string? ThumbnailBase64 { get; set; }
}
```

---

## Backend: Aggregation Scheduled Job

Add a hosted service that runs every 5 minutes to aggregate frames:

```csharp
public class ActivityAggregationJob : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), ct);
            // Find all active shifts
            // For each, aggregate last 5 minutes of frames into activity_summaries
        }
    }
}
```

---

## Backend: Daily Stats Endpoint

Add `GET /api/reports/daily/{employeeId}/{date}` response structure:

```json
{
  "date": "2026-03-07",
  "employee": { "id": "...", "fullName": "John Doe" },
  "shift": {
    "startedAt": "2026-03-07T09:00:00Z",
    "endedAt": "2026-03-07T17:15:00Z",
    "actualHours": 8.25
  },
  "totals": {
    "activeSeconds": 27900,
    "idleSeconds": 3900,
    "productivityScore": 0.78
  },
  "appBreakdown": [
    { "app": "code.exe", "displayName": "VS Code", "category": "coding",
      "seconds": 14400, "percent": 51.6 },
    { "app": "chrome.exe", "displayName": "Chrome (github.com)", "category": "browser_work",
      "seconds": 5400, "percent": 19.4 },
    { "app": "slack.exe", "displayName": "Slack", "category": "communication",
      "seconds": 3600, "percent": 12.9 }
  ],
  "hourlyProductivity": [
    { "hour": 9, "score": 0.85, "dominant": "coding" },
    { "hour": 10, "score": 0.82, "dominant": "coding" },
    { "hour": 11, "score": 0.61, "dominant": "browser_work" }
  ],
  "jiraTickets": ["PROJ-123", "PROJ-124"],
  "topKeywords": ["authentication", "middleware", "feature/login"]
}
```

---

## Testing Checklist

- [ ] App categories are correctly assigned (VS Code → "coding", Chrome → "browser")
- [ ] Idle frames are classified as "idle" regardless of active app
- [ ] OCR-extracted URLs populate `browser_domain` correctly
- [ ] Productivity scores are between 0.0 and 1.0
- [ ] 5-minute activity summaries are created in the DB
- [ ] Daily stats endpoint returns correct totals
- [ ] Screenshots are captured on category change AND at configured interval
- [ ] JIRA ticket IDs are extracted from OCR text

---

## Acceptance Criteria

1. Every activity frame has an `app_category` field populated
2. 5-minute activity summaries with dwell times and productivity scores exist in DB
3. Daily stats endpoint returns accurate time-on-app breakdown
4. Browser domains are extracted when OCR is enabled
5. Productivity score reflects actual work vs idle vs entertainment
