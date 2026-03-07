using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Surveil.Contracts;

namespace Surveil.Agent.Services;

/// <summary>HTTP client for communicating with the SurveilWin backend API.</summary>
public class ApiClient : IDisposable
{
    private readonly HttpClient _http;
    private string? _accessToken;
    private string? _refreshToken;
    private string? _employeeId;
    private string? _orgId;
    private readonly ConcurrentQueue<FrameUploadDto> _offlineBuffer = new();
    private const int MaxBufferSize = 1000;

    public string? EmployeeId => _employeeId;
    public string? OrgId => _orgId;
    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);

    public ApiClient(string baseUrl)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        _http.DefaultRequestHeaders.Add("X-Agent-Version", "agent-win-1.0.0");
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", new { email, password });
            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            _accessToken = result.GetProperty("accessToken").GetString();
            _refreshToken = result.GetProperty("refreshToken").GetString();

            var user = result.GetProperty("user");
            _employeeId = user.GetProperty("id").GetString();
            _orgId = user.GetProperty("orgId").GetString();

            SetAuthHeader();
            TokenStore.Save(_accessToken!, _refreshToken!);
            return true;
        }
        catch { return false; }
    }

    public bool LoadSavedTokens()
    {
        var saved = TokenStore.Load();
        if (saved == null) return false;
        _accessToken = saved.Value.AccessToken;
        _refreshToken = saved.Value.RefreshToken;
        SetAuthHeader();
        return true;
    }

    public async Task<string?> StartShiftAsync()
    {
        try
        {
            await EnsureValidTokenAsync();
            var response = await _http.PostAsJsonAsync("api/shifts/start", new { agentVersion = "agent-win-1.0.0" });
            if (!response.IsSuccessStatusCode) return null;
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            return result.GetProperty("id").GetString();
        }
        catch { return null; }
    }

    public async Task<bool> EndShiftAsync(string shiftId)
    {
        try
        {
            await EnsureValidTokenAsync();
            var response = await _http.PostAsJsonAsync($"api/shifts/{shiftId}/end", new { });
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<AgentConfig?> GetAgentConfigAsync()
    {
        if (string.IsNullOrEmpty(_orgId)) return null;
        try
        {
            await EnsureValidTokenAsync();
            var response = await _http.GetAsync($"api/organizations/{_orgId}/policy/agent-config");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<AgentConfig>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch { return null; }
    }

    public async Task<bool> UploadFrameBatchAsync(string shiftId, string sessionKey, List<FrameUploadDto> frames)
    {
        // Drain offline buffer first
        var buffered = new List<FrameUploadDto>();
        while (_offlineBuffer.TryDequeue(out var f)) buffered.Add(f);

        var allFrames = buffered.Concat(frames).ToList();
        if (allFrames.Count == 0) return true;

        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await EnsureValidTokenAsync();
                var payload = new { sessionKey, shiftId, frames = allFrames };
                var response = await _http.PostAsJsonAsync("api/activity/frames", payload);
                if (response.IsSuccessStatusCode) return true;
            }
            catch
            {
                if (attempt == 2)
                {
                    // Buffer frames for retry when back online
                    foreach (var f in frames)
                    {
                        if (_offlineBuffer.Count < MaxBufferSize)
                            _offlineBuffer.Enqueue(f);
                    }
                }
                await Task.Delay(2000);
            }
        }
        return false;
    }

    private async Task EnsureValidTokenAsync()
    {
        if (string.IsNullOrEmpty(_accessToken)) return;
        // Check expiry by parsing JWT payload
        try
        {
            var parts = _accessToken.Split('.');
            if (parts.Length < 2) return;
            var payload = System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String(PadBase64(parts[1])));
            var json = JsonSerializer.Deserialize<JsonElement>(payload);
            if (json.TryGetProperty("exp", out var expProp))
            {
                var exp = DateTimeOffset.FromUnixTimeSeconds(expProp.GetInt64());
                if (exp < DateTimeOffset.UtcNow.AddMinutes(5))
                    await RefreshTokenAsync();
            }
        }
        catch { }
    }

    private async Task<bool> RefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(_refreshToken)) return false;
        try
        {
            _http.DefaultRequestHeaders.Remove("Authorization");
            var response = await _http.PostAsJsonAsync("api/auth/refresh", new { refreshToken = _refreshToken });
            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            _accessToken = result.GetProperty("accessToken").GetString();
            _refreshToken = result.GetProperty("refreshToken").GetString();
            SetAuthHeader();
            TokenStore.Save(_accessToken!, _refreshToken!);
            return true;
        }
        catch { return false; }
    }

    private void SetAuthHeader()
    {
        _http.DefaultRequestHeaders.Remove("Authorization");
        if (!string.IsNullOrEmpty(_accessToken))
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
    }

    private static string PadBase64(string base64)
    {
        base64 = base64.Replace('-', '+').Replace('_', '/');
        return base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
    }

    public void Logout()
    {
        TokenStore.Clear();
        _accessToken = null;
        _refreshToken = null;
        _http.DefaultRequestHeaders.Remove("Authorization");
    }

    public void Dispose() => _http.Dispose();
}
