using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SurveilWin.Api.Services.AI;

public class OpenAiProvider : ILlmProvider
{
    public string ProviderName => "openai";
    private readonly HttpClient _http;
    private readonly string _model;

    public OpenAiProvider(IHttpClientFactory httpClientFactory, IConfiguration cfg)
    {
        _http = httpClientFactory.CreateClient("openai");
        _model = cfg["Ai:OpenAiModel"] ?? "gpt-4o-mini";
        var key = cfg["Ai:OpenAiApiKey"] ?? string.Empty;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
    }

    public async Task<string?> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        try
        {
            var request = new
            {
                model = _model,
                messages = new[] { new { role = "user", content = prompt } },
                max_tokens = 500,
                temperature = 0.7
            };
            var res = await _http.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", request, ct);
            if (!res.IsSuccessStatusCode) return null;
            var doc = await res.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
            return doc?.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }
        catch { return null; }
    }
}
