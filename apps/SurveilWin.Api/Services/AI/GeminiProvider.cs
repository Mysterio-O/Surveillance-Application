using System.Net.Http.Json;
using System.Text.Json;

namespace SurveilWin.Api.Services.AI;

public class GeminiProvider : ILlmProvider
{
    public string ProviderName => "gemini";
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiProvider(IHttpClientFactory httpClientFactory, IConfiguration cfg)
    {
        _http = httpClientFactory.CreateClient("gemini");
        _apiKey = cfg["Ai:GeminiApiKey"] ?? string.Empty;
        _model = cfg["Ai:GeminiModel"] ?? "gemini-2.0-flash";
    }

    public async Task<string?> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        try
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
            var request = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } }
            };
            var res = await _http.PostAsJsonAsync(url, request, ct);
            if (!res.IsSuccessStatusCode) return null;
            var doc = await res.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
            return doc?.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
        }
        catch { return null; }
    }
}
