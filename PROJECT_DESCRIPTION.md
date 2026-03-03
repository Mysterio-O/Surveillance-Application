# Surveil-Win Project Overview

This document describes the **Surveil-Win** repository in exhaustive detail.  It is intended for maintainers, developers and new contributors who need to understand the architecture, how to build/run the solution, and how to troubleshoot common issues.  Every component and small detail is covered.

---

## 🔍 High‑Level Summary

Surveil-Win is a Windows‑only application whose goal is to periodically capture the currently active window, extract textual and visual features, and produce human‑readable summaries of activity over time.  The prototype consists of:

1. **Agent.Win** – a class library with services for screen capture, activity/idle detection, OCR, and embedding via an ONNX model.
2. **Dashboard.Win** – a WPF application that uses the agent services to display live summaries and control capture.
3. **Runner** – a console application for headless / scheduled summarization that writes session JSON files.
4. **libs/Contracts**, **Processing**, **Utils** – shared libraries providing data contracts, a sliding‑window summarizer, and simple logging utilities.
5. **scripts/** – PowerShell helpers for setup and dev runs.
6. **data/sessions/** – output directory where thumbnails and summary JSON are written at runtime.
7. **models/onnx/** – optional third‑party CLIP ONNX model used by the embedding service.

Most of the user‑facing functionality is encapsulated in the agent services; the two executables simply wire them together in different ways.

---

## 📁 Repository Layout

```
SurveilWin.sln                ← Visual Studio solution
README.md                     ← minimal quick-start (original)
PROJECT_DESCRIPTION.md        ← this detailed document
apps/                         ← three application projects
    Agent.Win/               ← class library with Windows helpers
    Dashboard.Win/           ← WPF GUI app (MainWindow.xaml)
    Runner/                  ← CLI spinner generating JSON summaries
libs/                         ← reusable libraries
    Contracts/               ← DTOs and policy classes
    Processing/              ← summarizer implementation
    Utils/                   ← logging helper
models/onnx/                 ← CLIP ONNX model (downloaded by setup)
scripts/                      ← helper scripts (setup and run-dev)
data/                         ← runtime output (session data)
```

### Solution Projects

| Project          | Path                                 | Type        | Purpose                               |
|-----------------|--------------------------------------|-------------|---------------------------------------|
| Agent.Win       | `apps/Agent.Win/Agent.Win.csproj`    | Class lib   | Screen capture, OCR, embeddings, idle |
| Dashboard.Win   | `apps/Dashboard.Win/Dashboard.Win.csproj` | WPF app     | Interactive GUI front-end             |
| Runner          | `apps/Runner/Runner.csproj`          | Console app | Headless sampler with JSON output     |
| Contracts       | `libs/Contracts/Contracts.csproj`    | Class lib   | Data types: `FrameFeature`, `Summary`, `Policy` |
| Processing      | `libs/Processing/Processing.csproj`  | Class lib   | `SlidingSummarizer` logic             |
| Utils           | `libs/Utils/Utils.csproj`            | Class lib   | `Log` helper                          |

Projects are referenced in the solution file; building the solution will restore NuGet packages automatically.

---

## 🛠 Building & Running

### Prerequisites

1. **Windows 10/11** with .NET 8 SDK installed (setup script will install if missing).
2. **Tesseract OCR** (UB‑Mannheim build recommended) on `%PATH%`; used by `OcrService`.
3. (Optional) **ONNX CLIP model** – not required, but embeddings are disabled without it.  `setup.ps1` downloads it automatically.
4. **Visual Studio 2022/2023** or `dotnet` CLI for compilation.

### Setup Script

```powershell
> .\scripts\setup.ps1      # runs once per machine
```

The script will:

- Check/install .NET 8 via `winget`.
- Check/install Tesseract via `winget`.
- Create `models/onnx/` and download `clip-vit-b32.onnx` (~100 MB).
- Run `dotnet restore` on the solution.

You can skip individual steps by passing `-InstallDotNet:$false` or `-InstallTesseract:$false`.

### Development Run

The helper `scripts/run-dev.ps1` compiles the solution then launches the dashboard:

```powershell
> .\scripts\run-dev.ps1
```

Alternatively use the CLI directly:

```bash
# build the whole solution
dotnet build -c Debug

# run dashboard
dotnet run --project apps/Dashboard.Win/Dashboard.Win.csproj

# run headless runner to generate summary files
dotnet run --project apps/Runner/Runner.csproj
```

### Dashboard Usage

1. Click **Start** to begin capturing once per second.
2. Each captured frame is processed and the sliding summarizer emits a summary every ~30 s.
3. Summaries appear in the listbox; scroll back to review previous entries.
4. Click **Pause** to stop the loop (preserves previous summaries in memory).
5. Captured thumbnails and summaries are saved under `data/sessions/` by default.

### Runner Usage

The `Runner` executable behaves exactly like the dashboard loop but does not show a UI.  It writes JSON summary files to `data/sessions` and logs to the console.  A sample invocation is:

```powershell
cd "C:\Users\skrab\Downloads\surveil-win\surveil-win"
dotnet run --project .\apps\Runner\Runner.csproj
```

Use this for scheduled/background operation where no UI is needed.

---

## 🧠 Internal Components & Flow

### `Surveil.Agent.Services`

- `CaptureService` – obtains a bitmap of the current foreground window using GDI (`CopyFromScreen`).
  - **Note:** the README references the Windows Graphics Capture API; the current implementation is a simple GDI fallback for MVP.
  - `GetForegroundWindowTitle()` returns the window text via `GetWindowText`.
- `ActivityService` – determines idle state by calling `GetLastInputInfo` and comparing to a threshold.
- `OcrService` – writes the bitmap to a temporary PNG and invokes `tesseract` as an external process (`stdout` capture).  If Tesseract is missing or fails, a warning is logged and an empty string is returned.
- `EmbeddingService` – wraps an ONNX Runtime `InferenceSession`.  The constructor takes `modelPath` and loads the session if the file exists; otherwise, logs a warning and disables embedding (returns `null`).  `Encode` resizes the image to 224×224, normalizes and runs inference, returning a float array.

### `Surveil.Contracts`

Defines immutable data types used across components:

```csharp
public record FrameFeature(
    DateTime Timestamp,
    string ActiveApp,
    string WindowTitle,
    bool IsIdle,
    string OcrText,
    float[]? Embedding,
    string? ThumbnailPath
);

public record Summary(
    DateTime Start,
    DateTime End,
    string Narrative,
    string[] Evidence,
    double Confidence
);

public class Policy { ... }
```

`Policy` is currently unused but included for future filtering rules (allowed/denied apps, thumbnail storage).

### `Surveil.Processing.SlidingSummarizer`

- Maintains a rolling buffer of `FrameFeature` objects spanning a 60‑second window.
- When buffer span ≥30 s, it computes a summary:
  - Top three active apps by frame count.
  - Idle ratio (percent of frames marked idle).
  - First five non‑empty window titles.
  - Confidence heuristic: `max(0.4, 1 - idleRatio)`.
  - Narrative string and up to three distinct thumbnail paths as evidence.
- Emits the summary via the `OnSummary` event and clears the buffer.
- Logging call prints the narrative for debugging.

### `Surveil.Utils.Log`

Thread‑safe console logger with colorized levels (`Info`, `Warn`, `Error`).

### WPF & Console Loops

Both the dashboard (`MainWindow`) and runner (`Program.Main`) follow the same loop:
1. Capture foreground bitmap & window title.
2. Query idle status with a 60‑second threshold.
3. Run OCR asynchronously.
4. Generate embedding (if model present).
5. Save a thumbnail JPEG under `data/sessions/`.
6. Create `FrameFeature` and add to summarizer.
7. Delay ~1 s and repeat until cancellation (Ctrl+C or pause button).

Shared logic ensures parity between GUI and headless operation.

---

## ✅ Running the Project & Debugging

This section lists practical steps and solutions for common setup/runtime problems.

### 1. Setup fails or dependencies missing

- **`.NET SDK not found`**: rerun `setup.ps1` or install manually from https://dotnet.microsoft.com/download.
- **Tesseract errors**:
  - `OcrService` logs `[WARN] Tesseract not found or failed`: verify `tesseract --version` works in your PowerShell/Command prompt.  Reinstall the UB‑Mannheim build via `winget` or add to `%PATH%` manually.
  - If `tesseract` runs but returns gibberish, check language files (`.traineddata`) in its installation directory.
- **ONNX model missing**: the app logs a warning when the path does not exist.  `setup.ps1` will download it; you can also manually copy `clip-vit-b32.onnx` to `models/onnx/`.  Without it, embeddings are `null` and summaries will still work.

### 2. Build/compile errors

- Run `dotnet restore` or open the solution in Visual Studio and let NuGet restore packages.
- Ensure the `Microsoft.ML.OnnxRuntime` package is available (added by the project file).  If you change target frameworks, you may need to update package versions.
- If you receive errors about missing `System.Drawing.Common`, make sure the project targets Windows and includes the appropriate `PackageReference` (should already be present).

### 3. Recorder captures blank or wrong window

- The foreground window handle returned by `GetForegroundWindow()` may be `IntPtr.Zero` if no window is active (e.g. desktop). The service returns `null` and the loop skips that frame; this is normal.
- If you have elevated vs non‑elevated process mismatch, capturing may fail due to security. Run the app with the same privileges as the target window or disable UAC prompts.
- When the captured window is only 1×1 pixels or zero size, ensure the window is not minimized or hidden behind secure desktop (e.g. UAC prompts).

### 4. OCR returns empty or wrong text

- Check that the temporary PNG file is created and not corrupted; the `OcrService` deletes it after use but you can comment out the deletion for debugging.
- Use `tesseract` from the command line directly with sample images to verify languages/psm settings.
- For better results with languages other than English, change the `lang` parameter (default "eng").

### 5. Embeddings not generated

- If the ONNX model path is incorrect, the constructor logs a warning and `_session` is `null`.
- The tool searches in two locations: the executable's `models/onnx` subfolder and the repository root `models/onnx` (useful when running from the source directory during development).
- To replace with a different model, ensure the input preprocessing matches the model's requirements (currently 224×224 RGB normalized to [0,1]).

### 6. Dashboard UI freezes or crashes

- Long OCR or embedding calls are awaited in the UI thread; if you observe stutters when capturing, consider moving heavy work to a background thread or using `Task.Run`.
- Binding `/Dispatcher.Invoke` is used when summaries arrive; issues will appear if the dispatcher is shut down (e.g. during window closing). Cancel the loop (`_cts?.Cancel()`) in `OnClosing` if extending.
- WPF exceptions will typically show a red trace in the Output window. Enable first‑chance exceptions to catch them earlier.

### 7. Summary files and thumbnails

- By default everything is stored under `./data/sessions`.  This directory is created at runtime if missing.
- Thumbnails are named `thumb_yyyyMMdd_HHmmssfff.jpg` and summaries `summary_yyyyMMdd_HHmmss.json`.
- If you want to purge old data, you can manually delete files or add retention logic by editing `Runner` or `Dashboard` loops.

### 8. Logging and diagnostics

- Use the `Log.Info/Warn/Error` calls sprinkled throughout services to trace program flow.  In console mode you will see colored output; in WPF mode the console is typically hidden unless you launch with `dotnet run` from a terminal.
- Wrap suspicious blocks in additional try/catch and log exceptions to investigate crashes.

---

## 🔧 Extending the Codebase

Here are pointers for common extension scenarios:

- **Add new summarization logic**: modify `SlidingSummarizer.Emit` or create a new summarizer class; subscribe to `OnSummary` in the callers.
- **Filtering by policy**: the `Policy` class exists but is unused; you could pass a policy into the capture loop and drop features not matching allowed/denied apps.
- **Capture more data**: extend `FrameFeature` or add new service methods (e.g. audio levels, network usage).
- **Replace screenshot method**: use the Windows Graphics Capture API (COM/D3D) for higher‑quality/secure capture.  Update `CaptureService` accordingly and adjust project dependencies (Windows SDK references).
- **Cross‑platform**: the current code is heavily Windows‑specific (`System.Drawing`, P/Invoke). For Linux/Mac you would need to swap capture and idle detection layers.

---

## 📝 Notes & Miscellany

- The repository contains sample `data/sessions/summary_*.json` files produced during development (see `data/sessions` in the workspace). These can be used for inspection or unit testing.
- All paths are relative; the code assumes it is run from the repository root when using the fallback model path.  When packaging the apps for distribution you may need to adjust path resolution.
- The WPF project targets `net8.0-windows` with `<UseWPF>true</UseWPF>` in `Dashboard.Win.csproj`.
- `Agent.Win` and other libraries are plain `.NET` class libraries with `TargetFramework` set to `net8.0`.

---

## 🤖 Common Scenarios and Resolutions (FAQ)

| Problem                               | Likely Cause / Fix                                 |
|--------------------------------------|-----------------------------------------------------|
| No summaries appear in dashboard      | Loop not started; click **Start** or check for exceptions in console output. |
| OCR output is gibberish/blank         | Tesseract not installed or language data missing.   |
| Embedding log warns "model not found"| Place `clip-vit-b32.onnx` in `models/onnx` or set custom path. |
| Crash on startup (FileNotFound)       | Ensure `dotnet restore` succeeded; missing NuGet package. |
| WPF app shows a blank window          | Check `App.xaml` entry point; ensure `MainWindow` is the startup URI. |
| Runner halts after a few frames       | Run with higher verbosity or attach debugger; maybe `capture.CaptureForeground()` returning null repeatedly. |

---

This document should give you full visibility into the project layout, inner workings, how to operate it, and how to resolve typical problems.  Happy coding! 🚀
