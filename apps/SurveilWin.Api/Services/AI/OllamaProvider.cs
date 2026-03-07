using System.Net.Http.Json;
using System.Text.Json;

namespace SurveilWin.Api.Services.AI;

public class OllamaProvider : ILlmProvider
{
    public string ProviderName => "ollama";
    private readonly HttpClient _http;
    private readonly string _model;

    public OllamaProvider(IHttpClientFactory httpClientFactory, IConfiguration cfg)
    {
        _http = httpClientFactory.CreateClient("ollama");
        _model = cfg["Ai:OllamaModel"] ?? "llama3.2";
    }

    public async Task<string?> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        try
        {
            var request = new { model = _model, prompt, stream = false };
            var res = await _http.PostAsJsonAsync("/api/generate", request, ct);
            if (!res.IsSuccessStatusCode) return null;
            var doc = await res.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
            return doc?.RootElement.GetProperty("response").GetString();
        }
        catch { return null; }
    }
}
