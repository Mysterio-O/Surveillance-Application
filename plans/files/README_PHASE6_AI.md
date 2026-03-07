# Phase 6 — AI-Powered Daily Summaries (Low Cost)
## SurveilWin Production Platform

---

## Overview

This document instructs an AI agent to implement **AI-generated daily work summaries** for each employee at minimal cost. The AI reads aggregated text data (NOT images) and produces a professional, readable summary of the employee's workday.

**Cost goal:** Under $0.01 per employee per day. With 10 employees and 20 working days/month = under $2/month on AI costs.

---

## Prerequisites

- Phase 1 complete: Backend API with `daily_summaries` table
- Phase 4 complete: 5-minute activity windows with app categories, keywords, and productivity scores exist in DB

---

## Cost Analysis — Choosing the Right AI Provider

| Provider | Model | Input Cost | Output Cost | Monthly Cost (10 emp) | Quality |
|----------|-------|-----------|-------------|----------------------|---------|
| **Ollama** (local) | Llama 3.2 3B | **FREE** | **FREE** | **$0** | Good |
| **Ollama** (local) | Mistral 7B | **FREE** | **FREE** | **$0** | Better |
| **Google** | Gemini 2.0 Flash | $0.075/1M | $0.30/1M | ~$0.30 | Excellent |
| **OpenAI** | GPT-4o-mini | $0.15/1M | $0.60/1M | ~$0.60 | Excellent |
| **Groq** | Llama 3.1 70B | FREE tier | FREE tier | $0 (limited) | Very Good |
| **Anthropic** | Claude Haiku 3.5 | $0.80/1M | $4/1M | ~$2.40 | Excellent |

**Recommendation for startups:**
1. **Default: Ollama (local)** — completely free, run on the same server as the API
2. **Upgrade path: Gemini 2.0 Flash** — cheapest cloud option with excellent quality
3. **Enterprise: GPT-4o-mini or Claude Haiku** — best quality, still very affordable

**Input token estimate per employee per day:**
- App usage summary: ~300 tokens
- Window titles: ~200 tokens
- Keywords/JIRA tickets: ~100 tokens
- System prompt: ~300 tokens
- **Total input: ~900 tokens**
- Output summary: ~300 tokens
- **Total per request: ~1200 tokens**

---

## Architecture

### AI Provider Abstraction

Create `apps/SurveilWin.Api/Services/AI/ILlmProvider.cs`:

```csharp
public interface ILlmProvider
{
    string ProviderName { get; }
    bool IsConfigured { get; }
    Task<string?> CompleteAsync(string systemPrompt, string userPrompt, int maxTokens = 500);
}
```

### Provider Implementations

#### 1. Ollama Provider (FREE — Recommended Default)

```csharp
public class OllamaProvider : ILlmProvider
{
    public string ProviderName => "Ollama (Local)";
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly string _baseUrl;

    public OllamaProvider(string baseUrl = "http://localhost:11434", string model = "llama3.2")
    {
        _baseUrl = baseUrl;
        _model = model;
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public bool IsConfigured => TryPingOllama().GetAwaiter().GetResult();

    public async Task<string?> CompleteAsync(string systemPrompt, string userPrompt, int maxTokens = 500)
    {
        var request = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            stream = false,
            options = new { num_predict = maxTokens, temperature = 0.3 }
        };

        var response = await _http.PostAsJsonAsync("/api/chat", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
        return result?.Message?.Content;
    }

    private async Task<bool> TryPingOllama()
    {
        try
        {
            var response = await _http.GetAsync("/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
```

**Ollama setup (on the server):**
```bash
# Install Ollama
curl -fsSL https://ollama.ai/install.sh | sh

# Pull the model (one time, ~2GB disk)
ollama pull llama3.2

# Or for better quality (larger, ~4GB disk)
ollama pull mistral

# Ollama runs on http://localhost:11434 by default
```

#### 2. OpenAI Provider (GPT-4o-mini)

```csharp
public class OpenAiProvider : ILlmProvider
{
    public string ProviderName => "OpenAI GPT-4o-mini";
    private readonly HttpClient _http;
    private readonly string _model = "gpt-4o-mini";

    public OpenAiProvider(string apiKey)
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
    }

    public bool IsConfigured => !string.IsNullOrEmpty(
        _http.DefaultRequestHeaders.Authorization?.Parameter);

    public async Task<string?> CompleteAsync(string systemPrompt, string userPrompt, int maxTokens = 500)
    {
        var request = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            max_tokens = maxTokens,
            temperature = 0.3
        };

        var response = await _http.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OpenAiResponse>();
        return result?.Choices?[0]?.Message?.Content;
    }
}
```

#### 3. Google Gemini Provider (Cheapest Cloud Option)

```csharp
public class GeminiProvider : ILlmProvider
{
    public string ProviderName => "Google Gemini Flash";
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model = "gemini-2.0-flash-exp";

    public GeminiProvider(string apiKey)
    {
        _apiKey = apiKey;
        _http = new HttpClient();
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_apiKey);

    public async Task<string?> CompleteAsync(string systemPrompt, string userPrompt, int maxTokens = 500)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
        var request = new
        {
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = $"{systemPrompt}\n\n{userPrompt}" } } }
            },
            generationConfig = new { maxOutputTokens = maxTokens, temperature = 0.3 }
        };

        var response = await _http.PostAsJsonAsync(url, request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GeminiResponse>();
        return result?.Candidates?[0]?.Content?.Parts?[0]?.Text;
    }
}
```

---

## AI Provider Factory

Create `apps/SurveilWin.Api/Services/AI/LlmProviderFactory.cs`:

```csharp
public static class LlmProviderFactory
{
    public static ILlmProvider Create(AiConfig config)
    {
        return config.Provider?.ToLower() switch
        {
            "openai"  => new OpenAiProvider(config.OpenAiApiKey ?? throw new InvalidOperationException("OpenAI API key required")),
            "gemini"  => new GeminiProvider(config.GeminiApiKey ?? throw new InvalidOperationException("Gemini API key required")),
            "ollama"  => new OllamaProvider(config.OllamaBaseUrl ?? "http://localhost:11434", config.OllamaModel ?? "llama3.2"),
            _         => new OllamaProvider() // default: try local Ollama
        };
    }
}
```

**Config class:**
```csharp
public class AiConfig
{
    public string Provider { get; set; } = "ollama";  // ollama, openai, gemini
    public string? OpenAiApiKey { get; set; }
    public string? GeminiApiKey { get; set; }
    public string? OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public string? OllamaModel { get; set; } = "llama3.2";
    public bool EnableAiSummaries { get; set; } = true;
}
```

In `appsettings.json`:
```json
{
  "Ai": {
    "Provider": "ollama",
    "OllamaBaseUrl": "http://localhost:11434",
    "OllamaModel": "llama3.2",
    "OpenAiApiKey": "",
    "GeminiApiKey": "",
    "EnableAiSummaries": true
  }
}
```

**Never commit API keys to source control. Use environment variables:**
```bash
export AI__OPENAIapikey="sk-..."
export AI__GEMINIAPIKEY="AIza..."
```

---

## Daily Summary Generator

Create `apps/SurveilWin.Api/Services/AI/DailySummaryGenerator.cs`:

```csharp
public class DailySummaryGenerator
{
    private readonly ILlmProvider _llm;
    private readonly AppDbContext _db;

    public async Task<string?> GenerateAsync(Guid employeeId, DateOnly date)
    {
        // 1. Fetch daily stats from DB
        var dailyStats = await _db.DailySummaries
            .Include(d => d.Employee)
            .FirstOrDefaultAsync(d => d.EmployeeId == employeeId && d.Date == date);

        if (dailyStats == null) return null;

        // 2. Fetch 5-minute activity windows for the day
        var windows = await _db.ActivitySummaries
            .Where(s => s.EmployeeId == employeeId
                && s.WindowStart.Date == date.ToDateTime(TimeOnly.MinValue).Date)
            .OrderBy(s => s.WindowStart)
            .ToListAsync();

        // 3. Build structured input (TEXT ONLY — no images)
        var input = BuildSummaryInput(dailyStats, windows);

        // 4. Call AI
        var systemPrompt = GetSystemPrompt();
        var userPrompt = BuildUserPrompt(input);

        return await _llm.CompleteAsync(systemPrompt, userPrompt, maxTokens: 400);
    }

    private string GetSystemPrompt() => """
        You are a professional productivity analyst assistant. Your job is to write clear,
        objective, and concise daily work summaries for remote employees based on their
        activity data. Write in third person (e.g., "John worked on...").
        Keep the summary under 150 words. Focus on what was accomplished, not judgements.
        Be factual and professional. Avoid surveillance language.
        """;

    private string BuildUserPrompt(SummaryInput input) => $"""
        Write a daily work summary for {input.EmployeeName} on {input.Date:MMMM dd, yyyy}.

        Shift: {input.ShiftStart:HH:mm} – {input.ShiftEnd:HH:mm} ({input.TotalHours:F1} hours)
        Active time: {input.ActiveMinutes} minutes | Idle time: {input.IdleMinutes} minutes
        Productivity score: {input.ProductivityScore:P0}

        Application usage:
        {string.Join("\n", input.AppUsage.Select(a => $"- {a.DisplayName} ({a.Category}): {a.Minutes} minutes ({a.Percent:F0}%)"))}

        Top work keywords detected: {string.Join(", ", input.Keywords.Take(8))}
        JIRA tickets referenced: {string.Join(", ", input.JiraTickets)}

        Write the summary now:
        """;
}
```

---

## Prompt Engineering Examples

### Input Data (what gets sent to AI):
```
Shift: 09:00 – 17:30 (8.5 hours)
Active time: 445 minutes | Idle time: 65 minutes
Productivity score: 78%

Application usage:
- VS Code (coding): 248 minutes (56%)
- Chrome/github.com (browser_work): 89 minutes (20%)
- Slack (communication): 52 minutes (12%)
- Chrome/other (browser): 35 minutes (8%)
- Terminal (terminal): 21 minutes (5%)

Top work keywords: authentication, middleware, unit tests, CI pipeline
JIRA tickets referenced: PROJ-456, PROJ-457
```

### Expected AI Output (Ollama Llama 3.2):
```
John worked a productive 8.5-hour shift today, spending the majority of his time
(56%) in VS Code working on development tasks. His activity data indicates focused
work on authentication and middleware components, with references to JIRA tickets
PROJ-456 and PROJ-457.

He spent approximately 20% of his time reviewing code and resources on GitHub,
and 12% communicating via Slack. Terminal usage (5%) suggests active build and
deployment work. Total idle time was 65 minutes, distributed throughout the day.
Overall productivity score: 78%.
```

---

## Scheduled Batch Job

Create `apps/SurveilWin.Api/Services/AI/DailySummaryBatchJob.cs`:

```csharp
public class DailySummaryBatchJob : BackgroundService
{
    private readonly IServiceProvider _services;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Run at 00:30 each night (30 minutes after midnight)
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddDays(1).AddMinutes(30);
            var delay = nextRun - now;
            await Task.Delay(delay, ct);

            await GenerateSummariesForYesterdayAsync(ct);
        }
    }

    private async Task GenerateSummariesForYesterdayAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var generator = scope.ServiceProvider.GetRequiredService<DailySummaryGenerator>();

        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

        // Get all employees who had shifts yesterday
        var employees = await db.Shifts
            .Where(s => s.Date == yesterday && s.Status != ShiftStatus.Active)
            .Select(s => s.EmployeeId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var employeeId in employees)
        {
            // Check if summary already exists
            var exists = await db.DailySummaries
                .AnyAsync(d => d.EmployeeId == employeeId && d.Date == yesterday, ct);

            if (!exists)
            {
                var narrative = await generator.GenerateAsync(employeeId, yesterday);
                // Save to daily_summaries table
                await db.DailySummaries.AddAsync(new DailySummary
                {
                    EmployeeId = employeeId,
                    Date = yesterday,
                    AiNarrative = narrative,
                    AiModelUsed = generator.ModelName,
                    AiGeneratedAt = DateTime.UtcNow,
                    // ... other fields aggregated from activity_summaries
                }, ct);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
```

Register in `Program.cs`:
```csharp
builder.Services.AddHostedService<DailySummaryBatchJob>();
builder.Services.AddScoped<DailySummaryGenerator>();
builder.Services.AddSingleton<ILlmProvider>(sp =>
    LlmProviderFactory.Create(sp.GetRequiredService<IOptions<AiConfig>>().Value));
```

---

## On-Demand Summary Generation

Add an API endpoint to trigger summary generation immediately (useful for testing):

```
POST /api/reports/generate-ai-summary
Body: { "employeeId": "uuid", "date": "2026-03-07" }
Auth: OrgAdmin only
```

Also add:
```
POST /api/reports/regenerate-ai-summary/{employeeId}/{date}
Auth: OrgAdmin only (force regenerate even if summary exists)
```

---

## Per-Organization AI Configuration

Each organization configures its AI provider in OrgPolicy (from Phase 2):

```csharp
// When generating, use per-org config if available, fall back to global config
var orgPolicy = await _db.OrgPolicies.FindAsync(orgId);
var provider = orgPolicy?.AiProvider != null
    ? LlmProviderFactory.Create(orgPolicy.GetAiConfig())
    : _defaultProvider;
```

This allows:
- Org A to use their own OpenAI key
- Org B to use their own Gemini key
- Org C to use the default Ollama (free)

---

## Quality & Safety

### Content Safety
- Always use `temperature = 0.3` or lower — factual, consistent output
- Max output: 400 tokens — no rambling
- If AI returns empty or very short response, fall back to rule-based summary

### Rule-Based Fallback Summary

When AI is not configured or fails, generate a structured summary without AI:

```csharp
public string GenerateRuleBasedSummary(SummaryInput input)
{
    var topApp = input.AppUsage.OrderByDescending(a => a.Minutes).First();
    var idlePercent = (int)(input.IdleMinutes * 100.0 / (input.ActiveMinutes + input.IdleMinutes));

    return $"{input.EmployeeName} worked a {input.TotalHours:F1}-hour shift on {input.Date:MMMM dd}. " +
           $"Primary activity: {topApp.DisplayName} ({topApp.Percent:F0}% of active time). " +
           $"Productivity score: {input.ProductivityScore:P0}. " +
           $"Idle time: {idlePercent}%.";
}
```

---

## Ollama Docker Setup

Add Ollama to `docker-compose.yml` for easy self-hosting:

```yaml
  ollama:
    image: ollama/ollama
    ports:
      - "11434:11434"
    volumes:
      - ollama_data:/root/.ollama
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: 1
              capabilities: [gpu]  # Remove if no GPU

  ollama-setup:
    image: ollama/ollama
    depends_on:
      - ollama
    entrypoint: >
      /bin/sh -c "sleep 5 && ollama pull llama3.2"
    volumes:
      - ollama_data:/root/.ollama

volumes:
  postgres_data:
  ollama_data:
```

**With GPU:** Llama 3.2 3B generates a summary in ~2 seconds
**Without GPU (CPU only):** ~15-30 seconds per summary (still fine for batch jobs)

---

## Summary Quality Comparison

| Model | Summary Quality | Speed | Cost |
|-------|----------------|-------|------|
| Ollama llama3.2 (3B, CPU) | Good | 15-30s | FREE |
| Ollama mistral (7B, CPU) | Better | 30-60s | FREE |
| Ollama llama3.1 (8B, GPU) | Good | 2-3s | FREE |
| Gemini 2.0 Flash | Excellent | 1-2s | ~$0.001/summary |
| GPT-4o-mini | Excellent | 1-2s | ~$0.002/summary |

---

## Testing Checklist

- [ ] Ollama provider connects to local Ollama and generates a summary
- [ ] OpenAI provider uses correct model and API key
- [ ] Gemini provider formats request correctly for Gemini API
- [ ] Rule-based fallback generates readable summary when AI fails
- [ ] Batch job runs at 00:30 and generates summaries for all employees with completed shifts
- [ ] Summaries are stored in `daily_summaries` table with `ai_model_used` field
- [ ] On-demand summary generation endpoint works
- [ ] Per-org AI config overrides global config
- [ ] Daily summary appears on the web dashboard employee detail page
- [ ] Temperature 0.3 produces consistent, professional output

---

## Acceptance Criteria

1. At least one AI provider works end-to-end (Ollama recommended)
2. Batch job generates daily summaries for all employees every night
3. Summaries are readable, professional, and factual
4. Rule-based fallback produces output when AI is unavailable
5. Cost is under $0.01 per employee per day on any cloud provider
6. Per-org AI configuration works (different orgs can use different providers)
7. Summaries appear in the web dashboard (Phase 5 integration)

---

## Total System Cost Summary (Production)

For a startup with 10 employees running SurveilWin:

| Component | Monthly Cost |
|-----------|-------------|
| VPS (4GB RAM, 2 vCPU) | $12–20 |
| PostgreSQL (same VPS) | $0 |
| Ollama + Llama 3.2 (same VPS) | $0 |
| Web Dashboard hosting | $0 (same VPS, nginx) |
| Domain + SSL | $1–2 |
| **Total** | **$13–22/month** |

If using GPT-4o-mini instead of Ollama:
| AI costs (10 employees × 20 days) | ~$0.40/month |

**This makes SurveilWin one of the most cost-efficient employee monitoring platforms for remote startups.**
