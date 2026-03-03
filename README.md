# SurveilWin – Consent-First Activity Monitoring

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Status: MVP](https://img.shields.io/badge/Status-MVP-blue.svg)]()
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows-0078D4.svg)]()

## Overview

**SurveilWin** is a privacy-first, locally-run activity monitoring solution for Windows. It captures foreground window context, performs optical character recognition (OCR), and generates concise, explainable summaries of work activity—all on-device, with no cloud dependencies.

Designed for compliance and transparency, SurveilWin respects user consent, minimizes data collection by default, and provides managers with auditable, evidence-backed summaries.

---

## Key Features

✅ **Consent-First Capture**  
- System picker for display/window selection  
- Visible OS capture border (yellow, built-in to Windows.Graphics.Capture)  
- Pause button always accessible  
- Clear "What is collected" transparency page

✅ **Local, Private Processing**  
- All capture, OCR, and summarization happens on-device  
- No cloud dependencies; no data leaves your machine  
- Optional encryption at rest  

✅ **Intelligent Summaries**  
- Window/app tracking with timestamps  
- OCR text extraction (optional)  
- Zero-shot activity classification (code, browser, messaging, etc.)  
- Slide-window aggregation (configurable intervals)  
- Thumbnail snapshots (optional, off by default)  

✅ **Compliance Ready**  
- Built-in privacy notices and data subject access flows  
- GDPR/CCPA-aligned retention policies  
- Audit logs; export/delete APIs  
- DPIA and LIA templates provided  

✅ **Explainable AI**  
- Every summary includes timestamps, app name, OCR snippets, and thumbnails  
- Zero-shot labels with confidence scores  
- No black-box decisions  

---

## System Requirements

- **OS:** Windows 10/11 (x64)  
- **.NET:** .NET 6.0 or later  
- **Ram:** 4 GB minimum (8 GB recommended)  
- **Disk:** 1 GB free (for thumbnails and session data)  
- **GPU:** Optional (NVIDIA/Intel for faster embeddings via ONNX)  

### Optional Dependencies

- **Tesseract OCR** (included in package; or install via apt/brew for cross-platform)  
- **CLIP ONNX Model** (auto-downloaded on first run if embeddings enabled)  

---

## Installation & Setup

### 1. Clone the Repository

```bash
git clone https://github.com/Mysterio-O/Surveillance-Application.git
cd surveil-win
```

### 2. Restore Dependencies

```bash
dotnet restore
```

This downloads all NuGet packages (including Windows SDK, CsWinRT, Tesseract, ONNX Runtime).

### 3. Build the Solution

```bash
dotnet build SurveilWin.sln
```

Verify that all projects compile successfully.

### 4. (Optional) Configure Settings

Create or edit `appsettings.json` in the project root (or in each app folder):

```json
{
  "capture": {
    "fps": 1.0,
    "enabled": true
  },
  "ocr": {
    "enabled": true,
    "language": "eng"
  },
  "embeddings": {
    "enabled": true,
    "modelPath": "models/onnx/clip-vit-b32.onnx"
  },
  "thumbnails": {
    "enabled": false,
    "compression": "webp",
    "maxSizeBytes": 102400
  },
  "retention": {
    "thumbnails_days": 7,
    "summaries_days": 90
  }
}
```

---

## Quick Start

### Option A: Run the Dashboard (Interactive Mode)

1. **Start the Dashboard UI:**

```bash
dotnet run --project .\apps\Dashboard.Win\Dashboard.Win.csproj
```

2. **In the Dashboard:**
   - Click **"Pick Source"** to select display or window using the system picker
   - Click **"Start Capture"** to begin monitoring
   - Click **"Pause"** anytime to stop recording
   - View real-time summaries on the dashboard

3. **View Session Data:**
   - All summaries are saved to `data/sessions/*.json`
   - Thumbnails are in `data/sessions/thumbnails/`

### Option B: Run the Runner (Headless Mode)

Useful for scheduled capture or server deployments.

1. **Generate Summary from Existing Sessions:**

```bash
dotnet run --project .\apps\Runner\Runner.csproj
```

This reads all session files and generates rolling summaries.

2. **Run with Custom Arguments (Future):**

```bash
dotnet run --project .\apps\Runner\Runner.csproj -- --monitor 0 --duration 3600
```

(CLI support coming in Phase A2)

---

## Project Structure

```
surveil-win/
├── apps/
│   ├── Agent.Win/              # Windows capture service (GDI → WGC upgrade)
│   │   ├── Services/
│   │   │   ├── CaptureService.cs       # Frame capture logic
│   │   │   ├── ActivityService.cs      # Activity tracking
│   │   │   ├── OcrService.cs           # Tesseract OCR
│   │   │   └── EmbeddingService.cs     # CLIP ONNX embeddings
│   │   └── Agent.Win.csproj
│   ├── Dashboard.Win/          # WPF UI for monitoring
│   │   ├── Views/
│   │   │   ├── MainWindow.xaml         # Main dashboard
│   │   │   └── MainWindow.xaml.cs
│   │   └── Dashboard.Win.csproj
│   └── Runner/                 # Headless summarization engine
│       ├── Program.cs          # Entry point
│       └── Runner.csproj
├── libs/
│   ├── Contracts/              # Shared domain models
│   │   └── Contracts.cs        # Summary, CapturePolicy, etc.
│   ├── Processing/             # Core summarization logic
│   │   ├── SlidingSummarizer.cs        # Windowed aggregation
│   │   └── Processing.csproj
│   └── Utils/                  # Logging, helpers
│       ├── Log.cs
│       └── Utils.csproj
├── models/
│   └── onnx/
│       └── clip-vit-b32.onnx   # CLIP embedding model (optional)
├── data/
│   └── sessions/               # Output: summaries & thumbnails
├── scripts/
│   ├── run-dev.ps1             # Dev helper
│   └── setup.ps1               # Setup automation
├── FUTURE_ROADMAP.md           # Phase A–F engineering plan
├── PROJECT_DESCRIPTION.md      # Detailed architecture
├── README.md                   # This file
└── SurveilWin.sln              # Solution file

```

---

## How to Generate Summaries

### Automatic (Recommended)

**Summaries are generated automatically:**
- Dashboard mode: Real-time summaries displayed in the UI; also written to `data/sessions/summary_*.json`
- Runner mode: Aggregates all existing session data and outputs comprehensive summaries

### Manual

**To manually trigger summary generation:**

1. **Ensure capture has run:**
   ```bash
   dotnet run --project .\apps\Dashboard.Win\Dashboard.Win.csproj
   # ... let it capture for a few minutes ...
   # Press Pause or close the app
   ```

2. **Generate summaries via Runner:**
   ```bash
   dotnet run --project .\apps\Runner\Runner.csproj
   ```

3. **Check output:**
   ```bash
   ls data/sessions/
   # Should include: summary_20260303_060958.json, etc.
   ```

### Summary Format

Each summary JSON contains:

```json
{
  "sessionId": "session_20260303_060958",
  "startTime": "2026-03-03T06:09:58Z",
  "endTime": "2026-03-03T07:09:58Z",
  "durationSeconds": 3600,
  "summaries": [
    {
      "windowTitle": "Visual Studio Code",
      "app": "code.exe",
      "durationSeconds": 1200,
      "ocrText": "function calculate() { ... }",
      "topKeywords": ["calculate", "function", "return"],
      "activities": [
        {
          "category": "code editor",
          "confidence": 0.92
        }
      ],
      "thumbnailPath": "thumbnails/code-123.webp",
      "timestamps": {
        "start": "2026-03-03T06:10:00Z",
        "end": "2026-03-03T06:30:00Z"
      }
    }
  ],
  "totalIdleTime": 120,
  "isFullTrace": false
}
```

---

## Configuration Guide

### Capture Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `fps` | 1.0 | Frames per second (0.5–10 allowed) |
| `enabled` | true | Enable/disable capture globally |

### OCR Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `enabled` | true | Enable optical character recognition |
| `language` | "eng" | Tesseract language code (eng, fra, deu, etc.) |

### Embeddings & Classification

| Setting | Default | Description |
|---------|---------|-------------|
| `enabled` | true | Enable CLIP zero-shot classification |
| `modelPath` | `models/onnx/clip-vit-b32.onnx` | Path to CLIP ONNX model |

### Thumbnails

| Setting | Default | Description |
|---------|---------|-------------|
| `enabled` | false | **Off by default** (data minimization) |
| `compression` | "webp" | Format: webp (~30% smaller than JPEG) |
| `maxSizeBytes` | 102400 | Max thumbnail file size (100 KB) |

### Retention Policy

| Setting | Default | Description |
|---------|---------|-------------|
| `thumbnails_days` | 7 | Auto-delete thumbnails after N days |
| `summaries_days` | 90 | Auto-delete summaries after N days |

### Example: Disable Thumbnails, Keep 180 Days of Summaries

```json
{
  "thumbnails": {
    "enabled": false
  },
  "retention": {
    "summaries_days": 180
  }
}
```

---

## Troubleshooting

### Dashboard Won't Start

**Error:** `System.Runtime.InteropServices.COMException`

**Solution:**
1. Ensure Windows 10/11 with latest updates
2. Install Windows SDK (included in build; may require manual install if missing):
   ```bash
   dotnet workload restore
   ```

### Capture Says "Permission Denied"

**Error:** `Access Denied` when picking a window

**Solution:**
1. Ensure app is running with user privileges (not system/admin)
2. Grant Screen Recording permission (if prompted by Windows)
3. Check that Windows.Graphics.Capture is available (Windows 10+)

### No OCR Text in Summaries

**Cause:** Tesseract not properly installed or initialized

**Solution:**
1. Verify Tesseract package is installed:
   ```bash
   dotnet --version
   dotnet package search Tesseract.Interop
   ```
2. Ensure OCR is enabled in settings:
   ```json
   "ocr": { "enabled": true }
   ```
3. Check logs in `data/logs/` for Tesseract errors

### High CPU Usage

**Cause:** Too high FPS or embeddings running too frequently

**Solution:**
1. Reduce FPS:
   ```json
   "capture": { "fps": 0.5 }
   ```
2. Disable embeddings if not needed:
   ```json
   "embeddings": { "enabled": false }
   ```
3. Disable thumbnails:
   ```json
   "thumbnails": { "enabled": false }
   ```

### ONNX Model Not Found

**Error:** `File not found: models/onnx/clip-vit-b32.onnx`

**Solution:**
1. Download the model manually:
   ```bash
   # Create models/onnx directory
   mkdir -p models/onnx
   # Download CLIP from HuggingFace
   # (Link will be provided in Phase D)
   ```
2. Or disable embeddings:
   ```json
   "embeddings": { "enabled": false }
   ```

---

## Privacy & Compliance

### What We Collect

✅ **Collected (always auditable):**
- App name (process executable)
- Window title
- Timestamps
- Optional: OCR text
- Optional: Thumbnails

❌ **NOT collected:**
- Keyboard input / keystrokes
- Audio / microphone
- Mouse clicks (velocity tracked, not position)
- Clipboard / copy-paste history
- Browser history (URLs not extracted from title alone)

### Data Storage

- All data stored locally in `data/` directory
- No cloud sync or external APIs called
- Encryption at-rest available via OS (BitLocker on Windows)
- **Retention:** Thumbnails 7 days (default), summaries 90 days (configurable)

### Transparency & Consent

- **Onboarding notice** explains what's captured and why
- **"What We Collect" page** in Dashboard shows current settings
- **Pause button** always available to stop recording
- **Export/Delete** APIs for data subject access requests (Phase F)

### GDPR/CCPA Compliance

This tool is designed to be **privacy-by-default**:
- Minimal data collection (no tracking URLs, clipboard, input)
- Transparent notices and user controls
- Retention policies and auto-deletion
- Audit logs for transparency
- Access/deletion APIs

**Before deploying to employees, consult:**
- UK ICO: [Employee Monitoring – Is It Right for Your Business?](https://ico.org.uk/for-organisations/employee-monitoring/)
- GDPR Local: [Practical LIA Templates](https://gdprlocal.com)
- Your legal/compliance team for local regulations

---

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature`
3. Commit with clear messages: `git commit -am "Add feature X"`
4. Push and open a Pull Request

### Development Workflow

```bash
# Clone and restore
git clone https://github.com/Mysterio-O/Surveillance-Application.git
cd surveil-win
dotnet restore

# Run tests (when available)
dotnet test

# Build
dotnet build SurveilWin.sln

# Run locally
dotnet run --project .\apps\Dashboard.Win\Dashboard.Win.csproj
```

### Roadmap

See [FUTURE_ROADMAP.md](FUTURE_ROADMAP.md) for:
- Phase A: Windows hardening (WGC upgrade)
- Phase B: macOS support
- Phase C: Linux support
- Phase D: gRPC streaming architecture
- Phase E: Intelligence & labeling
- Phase F: Compliance & policy

---

## License

This project is licensed under the **MIT License**. See [LICENSE](LICENSE) for details.

---

## References & Resources

### Windows Capture
- [Windows.Graphics.Capture API](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture)
- [Screen Capture Guidance](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture)
- [Windows Graphics Capture Blog](https://blogs.windows.com/windowsdeveloper/2019/09/16/new-ways-to-do-screen-capture/)

### OCR & Embeddings
- [Tesseract OCR Manual](https://tesseract-ocr.github.io/tessdoc/)
- [Tesseract GitHub](https://github.com/tesseract-ocr/tesseract)
- [CLIP Paper](https://arxiv.org/abs/2103.00020)
- [CLIP Code](https://github.com/openai/CLIP)

### Privacy & Compliance
- [UK ICO: Employee Monitoring](https://ico.org.uk/for-organisations/employee-monitoring/)
- [GDPR Local: LIA & Consent Templates](https://gdprlocal.com)
- [EDPB Guidelines](https://edpb.ec.europa.eu/)

---

## Support & Contact

- **Issues:** [GitHub Issues](https://github.com/Mysterio-O/Surveillance-Application/issues)
- **Discussions:** [GitHub Discussions](https://github.com/Mysterio-O/Surveillance-Application/discussions)

---

**Status:** MVP (Windows) | **Last Updated:** March 2026 | **Maintainer:** [@Mysterio-O](https://github.com/Mysterio-O)
