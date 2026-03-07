# Phase 3 — Agent Upgrade: Cloud Upload, Shift Tracking & Auth
## SurveilWin Production Platform

---

## Overview

This document instructs an AI agent to **upgrade the existing `Agent.Win` and `Dashboard.Win`** to support:
- Employee login (JWT auth)
- Shift start/end (sends to backend API)
- Uploading all activity frames to the central backend instead of saving to local disk
- Fetching org monitoring policy from backend
- Secure token storage on employee machine

**Existing code to modify:** `apps/Agent.Win/`, `apps/Dashboard.Win/`, `libs/Contracts/`

---

## Prerequisites

- Phase 1 and 2 complete: Backend API is running and accepting frame uploads
- Backend URL is known: e.g., `https://api.surveilwin.com` or `http://localhost:8080`

---

## Summary of Changes

| Component | Change |
|-----------|--------|
| `Agent.Win` | Add `ApiClient` service to upload frames + shift control |
| `Agent.Win` | Add `TokenStore` service for secure JWT storage (Windows DPAPI) |
| `Dashboard.Win` | Replace start/pause with login screen → shift start → shift end flow |
| `Dashboard.Win` | Remove local JSON file saving (data goes to API now) |
| `Dashboard.Win` | Add "Shift Active" indicator with elapsed time |
| `libs/Contracts` | Add `AgentConfig` from API response |
| `appsettings.json` | Add `ApiBaseUrl` setting |

---

## New Service: ApiClient

### Create `apps/Agent.Win/Services/ApiClient.cs`

```csharp
public class ApiClient : IDisposable
{
    private readonly HttpClient _http;
    private string? _accessToken;
    private string? _refreshToken;

    public ApiClient(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _http.DefaultRequestHeaders.Add("X-Agent-Version", GetAgentVersion());
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    /// Login with employee credentials. Returns true on success.
    public async Task<bool> LoginAsync(string email, string password);

    /// Upload a batch of frames for the active shift.
    /// Returns true on success, false if needs retry.
    public async Task<bool> UploadFrameBatchAsync(string shiftId, string sessionKey, List<FrameUploadDto> frames);

    /// Upload a pre-aggregated summary window.
    public async Task<bool> UploadSummaryAsync(SummaryUploadDto summary);

    /// Start a shift. Returns ShiftId (GUID) on success, null on failure.
    public async Task<string?> StartShiftAsync();

    /// End the active shift. Returns true on success.
    public async Task<bool> EndShiftAsync(string shiftId);

    /// Fetch org policy (agent config). Returns AgentConfig or null.
    public async Task<AgentConfig?> GetAgentConfigAsync();

    /// Refresh the JWT using the stored refresh token.
    private async Task<bool> RefreshTokenAsync();

    /// Attach Bearer token to request headers.
    private void SetAuthHeader();

    private string GetAgentVersion() => "agent-win-1.0.0";

    public void Dispose() => _http.Dispose();
}
```

### Key Implementation Details

**Token management:**
- On every API call, check if JWT is expired (parse exp claim from token payload without full validation)
- If expired, call `RefreshTokenAsync()` before retrying
- If refresh fails (refresh token expired), show re-login dialog

**Retry logic:**
- Retry failed uploads up to 3 times with 2-second delay
- If offline, queue frames in memory (up to 500 frames); retry when connection restored
- Log upload errors to local file (`data/logs/upload_errors.log`)

**Offline buffer:**
- Use `ConcurrentQueue<FrameUploadDto>` for buffered frames
- On each upload cycle, drain the queue first, then add new frames
- If buffer exceeds 1000 frames, drop oldest frames (with warning log)

---

## New Service: TokenStore

### Create `apps/Agent.Win/Services/TokenStore.cs`

Use Windows DPAPI (`System.Security.Cryptography.ProtectedData`) to encrypt tokens at rest:

```csharp
public static class TokenStore
{
    private static readonly string StorePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SurveilWin", "agent_tokens.dat");

    /// Save access and refresh tokens (encrypted with DPAPI).
    public static void Save(string accessToken, string refreshToken);

    /// Load saved tokens (decrypt with DPAPI). Returns null if not found.
    public static (string AccessToken, string RefreshToken)? Load();

    /// Clear saved tokens (on logout or deactivation).
    public static void Clear();
}
```

**DPAPI scope:** Use `DataProtectionScope.CurrentUser` so tokens are tied to the Windows user account.

**Storage location:** `%LOCALAPPDATA%\SurveilWin\agent_tokens.dat`

---

## Dashboard.Win Login Screen

### Modify `apps/Dashboard.Win/Views/MainWindow.xaml`

Replace the current "Start/Pause" UI flow with a **login-first flow**:

#### States / Views

The main window should manage three states using a `ContentControl` or `Grid` with `Visibility` bindings:

1. **LoginView** — shown when not authenticated
2. **ShiftView** — shown after login, before shift starts
3. **MonitoringView** — shown during active shift (current dashboard content)

#### LoginView XAML

Add a login panel inside the existing XAML (or as a separate UserControl):
```xaml
<!-- Login View -->
<Grid x:Name="LoginGrid" Visibility="Visible">
    <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center" Width="380">
        <!-- Logo/Brand -->
        <TextBlock Text="SurveilWin" FontSize="28" FontWeight="Bold"
                   Foreground="#89b4fa" HorizontalAlignment="Center" Margin="0,0,0,8"/>
        <TextBlock Text="Employee Monitoring" FontSize="14"
                   Foreground="#6c7086" HorizontalAlignment="Center" Margin="0,0,0,32"/>

        <!-- Server URL field (collapsible, for initial setup) -->
        <TextBox x:Name="ServerUrlBox" Margin="0,0,0,12"
                 Text="http://localhost:8080" Style="{StaticResource InputStyle}"/>

        <!-- Email field -->
        <TextBox x:Name="EmailBox" Margin="0,0,0,12"
                 Style="{StaticResource InputStyle}"/>

        <!-- Password field -->
        <PasswordBox x:Name="PasswordBox" Margin="0,0,0,24"
                     Style="{StaticResource PasswordStyle}"/>

        <!-- Login button -->
        <Button x:Name="LoginButton" Content="Sign In"
                Click="LoginButton_Click" Style="{StaticResource PrimaryButton}"/>

        <!-- Error message -->
        <TextBlock x:Name="LoginErrorText" Foreground="#f38ba8"
                   Margin="0,12,0,0" TextWrapping="Wrap" HorizontalAlignment="Center"/>
    </StackPanel>
</Grid>
```

#### ShiftView XAML (shown after login, before shift)

```xaml
<Grid x:Name="ShiftGrid" Visibility="Collapsed">
    <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
        <!-- Greeting -->
        <TextBlock x:Name="GreetingText" FontSize="22"
                   Foreground="#cdd6f4" HorizontalAlignment="Center"/>

        <!-- Organization name -->
        <TextBlock x:Name="OrgNameText" FontSize="14"
                   Foreground="#6c7086" HorizontalAlignment="Center" Margin="0,4,0,32"/>

        <!-- Start shift button -->
        <Button x:Name="StartShiftButton" Content="▶  Start Shift"
                Click="StartShift_Click" Width="200"
                Style="{StaticResource PrimaryButton}"/>

        <!-- Logout link -->
        <Button x:Name="LogoutButton" Content="Sign Out"
                Click="Logout_Click" Style="{StaticResource GhostButton}" Margin="0,16,0,0"/>
    </StackPanel>
</Grid>
```

#### MonitoringView (existing dashboard + shift header)

Add to the top of the existing dashboard:
```xaml
<!-- Shift status bar -->
<Border Background="#313244" Padding="16,8" DockPanel.Dock="Top">
    <Grid>
        <StackPanel Orientation="Horizontal">
            <Ellipse Width="8" Height="8" Fill="#a6e3a1" VerticalAlignment="Center" Margin="0,0,8,0"/>
            <TextBlock x:Name="ShiftStatusText" Text="Shift Active"
                       Foreground="#a6e3a1" FontWeight="SemiBold"/>
            <TextBlock x:Name="ShiftElapsedText"
                       Foreground="#6c7086" Margin="12,0,0,0"/>
        </StackPanel>
        <Button x:Name="EndShiftButton" Content="End Shift"
                Click="EndShift_Click" HorizontalAlignment="Right"
                Style="{StaticResource DangerButton}"/>
    </Grid>
</Border>
```

---

## Dashboard.Win Code-Behind Changes

### Modify `apps/Dashboard.Win/Views/MainWindow.xaml.cs`

**New fields:**
```csharp
private ApiClient? _apiClient;
private string? _activeShiftId;
private string? _activeSessionKey;
private DateTime _shiftStartTime;
private readonly List<FrameUploadDto> _pendingFrames = new();
private const int UploadBatchSize = 60; // upload every 60 frames (~1 minute at 1FPS)
```

**New methods:**

```csharp
private async void LoginButton_Click(object sender, RoutedEventArgs e)
{
    // Validate inputs
    // Create ApiClient with server URL
    // Call apiClient.LoginAsync(email, password)
    // On success: save tokens, load AgentConfig, show ShiftGrid
    // On failure: show LoginErrorText with message
}

private async void StartShift_Click(object sender, RoutedEventArgs e)
{
    // Call apiClient.StartShiftAsync()
    // On success: store shiftId, show MonitoringView, start capture loop
    // Start shift elapsed time timer (updates every second)
}

private async void EndShift_Click(object sender, RoutedEventArgs e)
{
    // Confirm dialog: "Are you sure you want to end your shift?"
    // Upload remaining pending frames
    // Call apiClient.EndShiftAsync(shiftId)
    // Stop capture loop
    // Show ShiftGrid (ready for next day)
}

private async Task UploadPendingFramesAsync()
{
    if (_pendingFrames.Count == 0) return;
    var batch = _pendingFrames.ToList();
    _pendingFrames.Clear();
    await _apiClient!.UploadFrameBatchAsync(_activeShiftId!, _activeSessionKey!, batch);
}
```

**Modify `CaptureFrameAsync` to build `FrameUploadDto`:**
```csharp
// Instead of creating a Summary JSON file, add to _pendingFrames
_pendingFrames.Add(new FrameUploadDto
{
    CapturedAt = DateTime.UtcNow,
    ActiveApp = processName,
    WindowTitle = windowTitle,
    IsIdle = isIdle,
    OcrText = ocrText,
    MonitorIndex = monitorInfo?.Index,
    CursorX = cursorPos.X,
    CursorY = cursorPos.Y,
    ThumbnailBase64 = config.SaveThumbnails ? ConvertToBase64WebP(bitmap) : null
});

// When batch is full, upload
if (_pendingFrames.Count >= UploadBatchSize)
    await UploadPendingFramesAsync();
```

**Remove local file writing** (the old `OnSummaryArrived` method that saves JSON files is no longer needed). The `SlidingSummarizer` can still be used to compute UI summaries, but do not write to `data/sessions/`.

---

## Apply Org Policy from Backend

After successful login, fetch the org policy:

```csharp
var agentConfig = await _apiClient.GetAgentConfigAsync();
if (agentConfig != null)
{
    // Override local config with server-side policy
    _config.CaptureFps = agentConfig.CaptureFps;
    _config.EnableOcr = agentConfig.EnableOcr;
    _config.SaveThumbnails = agentConfig.EnableScreenshots;
    _config.AllowedApps = agentConfig.AllowedApps;
    _config.DeniedApps = agentConfig.DeniedApps;
}
```

---

## New Contracts

Add to `libs/Contracts/Contracts.cs`:

```csharp
// Config received from backend after login
public class AgentConfig
{
    public double CaptureFps { get; set; } = 1.0;
    public bool EnableOcr { get; set; } = true;
    public bool EnableScreenshots { get; set; } = false;
    public int ScreenshotIntervalMinutes { get; set; } = 5;
    public List<string> AllowedApps { get; set; } = new();
    public List<string> DeniedApps { get; set; } = new();
    public double ExpectedShiftHours { get; set; } = 8.0;
}

// Frame data for API upload
public class FrameUploadDto
{
    public DateTime CapturedAt { get; set; }
    public string ActiveApp { get; set; } = "";
    public string WindowTitle { get; set; } = "";
    public bool IsIdle { get; set; }
    public string? OcrText { get; set; }
    public int? MonitorIndex { get; set; }
    public int? CursorX { get; set; }
    public int? CursorY { get; set; }
    public string? ThumbnailBase64 { get; set; } // base64 WebP
}
```

Add to `appsettings.json`:
```json
{
  "ApiBaseUrl": "http://localhost:8080",
  ...existing settings...
}
```

---

## System Tray Integration (Optional Enhancement)

To make the agent feel like a proper Windows app, add system tray support:

1. Add `System.Windows.Forms` reference to `Dashboard.Win.csproj`
2. Create `NotifyIcon` in `App.xaml.cs`:
   - Tray icon: custom SurveilWin icon
   - Tray menu: "Open Dashboard", "Shift Active / Not Active", "Exit"
3. When user closes the main window, minimize to tray (don't exit)
4. Only exit when "Exit" is clicked from tray menu
5. Show tray notification when shift starts/ends

---

## Screenshot Upload

When `EnableScreenshots = true` and the screenshot interval elapses:

```csharp
private string? CaptureScreenshotBase64(Bitmap bitmap)
{
    // Resize to max 1280px width (preserve aspect ratio)
    // Convert to WebP format (use ImageSharp or built-in)
    // Return base64 string
    // Keep under 100KB per screenshot
}
```

**Add NuGet package:**
```bash
dotnet add apps/Agent.Win package SixLabors.ImageSharp --version 3.1.*
dotnet add apps/Agent.Win package SixLabors.ImageSharp.Drawing --version 2.1.*
```

Use `SixLabors.ImageSharp` for WebP encoding (cross-platform, no native deps):
```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;

public string BitmapToBase64WebP(Bitmap gdiBitmap)
{
    // Convert System.Drawing.Bitmap → ImageSharp Image
    // Resize to max 1280 width
    // Encode to WebP with quality 60
    // Return base64
}
```

---

## Runner.cs Update

Also update `apps/Runner/Program.cs` with the same API client integration for **headless mode** (useful for automated/CI environments or if the employee uses the headless runner instead of the WPF app):

```csharp
// Add login flow at startup:
// 1. Try to load tokens from TokenStore
// 2. If no tokens, prompt email/password in console
// 3. Login and start shift
// 4. Upload frames to API in the same loop
// 5. On SIGINT (Ctrl+C), end shift and upload remaining frames
```

---

## Testing Checklist

- [ ] Employee can log in from the agent with email/password
- [ ] Login failure shows clear error message
- [ ] JWT token is saved to DPAPI-encrypted file after login
- [ ] Agent auto-refreshes JWT token when it expires (without requiring re-login)
- [ ] "Start Shift" creates a shift record in the backend
- [ ] Frames are uploaded in batches of ~60 frames
- [ ] "End Shift" uploads remaining frames, ends shift in backend
- [ ] Org policy is applied from backend (overrides local config)
- [ ] If API is unreachable, frames are buffered locally and retried
- [ ] Screenshots are base64-encoded WebP and uploaded with frames
- [ ] No JSON files are written to local `data/sessions/` anymore

---

## Acceptance Criteria

1. Employee can complete full flow: login → start shift → work (frames uploaded) → end shift
2. All frames during shift are persisted in the backend database
3. Org monitoring policy is enforced on the agent
4. Token is securely stored and auto-refreshed
5. Offline buffering works: agent buffers frames when server is unreachable, uploads when back online
6. Application minimizes to system tray and does not close unexpectedly
