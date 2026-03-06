# SurveilWin Product Roadmap

## Executive Summary

SurveilWin is evolving from a Windows-only MVP to a robust, cross-platform, privacy-aware, and scalable product. This roadmap outlines the engineering plan to evolve the current local, consent-first capture system into a multi-platform solution with streaming architecture, intelligent summarization, and full compliance readiness.

**Timeline:** 6–8 weeks (phased)
**Target Platforms:** Windows (hardened), macOS, Linux (Wayland)
**Architecture:** Single-process (local) → gRPC split → Kafka backbone (optional)

---

## Current Baseline (Updated March 2026)

### Completed (v0.2)

The following items have been shipped and are fully functional:

- **Multi-Monitor Cursor Tracking**
  - `CaptureService` detects which screen the cursor currently resides on via `GetCursorPos` + `MonitorFromPoint` + `GetMonitorInfo`
  - `CaptureCursorScreen()` captures the full monitor under the cursor — works across any number of connected displays
  - `GetAllMonitors()` enumerates every connected display for the Dashboard UI
  - Every `FrameFeature` records `MonitorDevice`, `MonitorIndex`, `CursorX`, `CursorY`

- **App Allow / Deny Lists + Auto-Pause (Phase A2 ✅)**
  - `PolicyService` enforces per-process allow and deny lists from `appsettings.json`
  - Denied apps are silently skipped; allow-list mode restricts capture to named processes only
  - Both Dashboard and Runner respect policy before capturing each frame

- **Full Trace Mode (Phase A3 ✅)**
  - `Summary` record extended with `SessionId`, `IsFullTrace`, and optional `Frames[]` array
  - When `FullTraceMode: true`, every emitted summary JSON embeds the full per-frame data
  - Gated behind config; off by default for data minimization

- **Configuration System (New ✅)**
  - `ConfigService` loads `appsettings.json` from multiple search paths (output dir, source tree, working dir)
  - `AppConfig` class with 16 settings: FPS, OCR language, model path, thumbnails, retention, allow/deny lists, idle threshold, adaptive FPS, full-trace, etc.
  - Shared config file copied to output directory by both Dashboard and Runner csproj

- **Data Retention (New ✅)**
  - `RetentionService` auto-deletes thumbnails and summary JSON files exceeding configured retention windows
  - Runs on startup; configurable via `ThumbnailRetentionDays` and `SummaryRetentionDays`

- **Adaptive FPS (Phase A4 partial ✅)**
  - Capture delay doubles automatically when the user is idle
  - Controlled by `AdaptiveFps` and `IdleThresholdSeconds` settings

- **Session Management (New ✅)**
  - Every run generates a unique `session_YYYYMMDD_HHmmss` ID
  - Session ID embedded in every summary JSON and shown in Dashboard status bar
  - Thread-safe buffer in `SlidingSummarizer` with improved confidence heuristic

- **Dashboard UI Overhaul (New ✅)**
  - Dark-themed WPF layout with live status chips: running indicator, active monitor, active app, FPS
  - Right-hand settings panel with toggles for thumbnails, full-trace, OCR
  - Connected-monitor list populated from `GetAllMonitors()`
  - Status bar showing session ID and running frame counter
  - Activity summary list with styled items

- **Contracts Expansion (New ✅)**
  - `MonitorInfo` and `ScreenRect` records for multi-monitor support
  - `FrameFeature` extended with monitor device, index, cursor coordinates
  - `Summary` extended with session ID, full-trace flag, frames array

### Components

- `Agent.Win`: Capture service (multi-monitor cursor tracking), activity tracking, OCR, embeddings, config, policy, retention
- `Dashboard.Win`: WPF UI for monitoring, configuration, and live status
- `Runner`: Headless summarization engine with config/policy support
- `Processing`: Sliding-window aggregation logic with session tracking
- `Contracts`: Shared domain models and configuration types

---

## Next Upcoming Updates

### **Phase A (Remaining): Windows Hardening** — Priority: HIGH

The following items from Phase A are not yet implemented and are the immediate next steps:

#### A1. Switch Capture to Windows.Graphics.Capture (WGC)

**Status:** NOT STARTED
**Why:** System picker, high-performance frame capture, visible/auditable yellow capture border.

**Changes needed:**

- **Agent.Win.csproj**
  - Add `Microsoft.Windows.SDK.BuildTools` and `Microsoft.Windows.CsWinRT` NuGet packages

- **CaptureService.cs**
  - Add WGC pipeline alongside existing GDI:
    - `GraphicsCapturePicker` → user selects display or window
    - `GraphicsCaptureSession` + `Direct3D11CaptureFramePool` for frame capture
    - Convert Direct3D frames to CPU bitmap for OCR/embedding pipeline
  - Keep GDI as fallback for systems without WGC support
  - Store selected `GraphicsCaptureItem` metadata for headless mode

- **Dashboard.Win**
  - Add "Pick Source" button that invokes `GraphicsCapturePicker`
  - Display selected source name; show that OS capture border is active

- **Runner**
  - Add CLI options: `--monitor <index>` or `--window <title>` for headless source selection
  - Load last-selected source from config if no CLI args provided

#### A4. Performance & Stability (Remaining Items)

**Status:** PARTIALLY DONE (adaptive FPS shipped; items below remain)

- **Scene-Change Detection (SSIM):** Compare consecutive frames; burst to higher FPS on detected change, drop to low FPS on static content
- **Selective ROI OCR:** Only OCR title bars and document areas instead of the entire frame (~50% latency reduction)
- **WebP Thumbnails:** Replace JPEG thumbnails with WebP for ~30% disk savings
- **Frame Pooling:** Reuse bitmap buffers to reduce GC pressure

#### A5. Consent & Privacy UX (Remaining Items)

**Status:** PARTIALLY DONE (settings panel toggles shipped; items below remain)

- **Onboarding Notice Page:** First-run dialog explaining what SurveilWin captures, where data is stored, and how to pause or delete
- **"What is Collected" Dedicated Page:** Full-page WPF view with plain-language explanation of each feature, toggle controls, retention periods, and deletion instructions

---

### **Phase E: Intelligence & Labeling** — Priority: HIGH

*Goal: Add zero-shot labeling and project mapping for richer, auditable summaries.*

#### E1. Zero-Shot Labels via CLIP

**Status:** NOT STARTED

Use the existing CLIP ONNX model to classify activity screenshots into categories:

```
prompt_categories = [
    "code editor", "IDE", "slides presentation", "spreadsheet",
    "browser window", "email or messaging", "video or social media",
    "documentation", "meetings or video calls", "terminal or command line"
]
```

**Implementation:**
- Compute embedding for each prompt once (cache at startup)
- For each frame, compute cosine similarity between frame embedding and prompt embeddings
- Return top 3 categories with confidence scores
- Store in Summary: `"activities": [{"category": "code editor", "confidence": 0.92}]`

**Files to create/modify:**
| File | Change |
|------|--------|
| `libs/Processing/CLIPClassifier.cs` | (New) Zero-shot prompt embedding + cosine similarity |
| `libs/Processing/SlidingSummarizer.cs` | Integrate CLIP categories into Summary narrative |
| `libs/Contracts/Contracts.cs` | Add `ActivityLabel` record; extend `Summary` with `Activities[]` |

#### E2. OCR Keyword Extraction & Project Mapping

**Status:** NOT STARTED

- Extract tokens from OCR text: JIRA-123, git repo names, file paths
- Maintain regex-based project dictionary in config
- Tag summaries with matched project names and identifiers

**Files to create/modify:**
| File | Change |
|------|--------|
| `libs/Processing/ProjectMapper.cs` | (New) OCR token extraction + regex dictionary matching |
| `libs/Contracts/Contracts.cs` | Add `Project`, `Identifiers[]` fields to Summary |
| `appsettings.json` | Add `Projects` section with pattern definitions |

#### E3. Summarization Model (Extensible)

**Status:** NOT STARTED

- Rule-first logic: keyword frequency, app dwell time, monitor switches
- Plug-in interface for local LLM (e.g., Ollama, llama.cpp) — optional, off by default

---

### **Phase F: Privacy, Compliance & Policy** — Priority: MEDIUM

*Goal: Ship compliance-ready by default; support EU, UK, and California regulations.*

#### F1. Privacy Notice & DPIA Templates

- Dashboard page: "What We Collect" (plain-language explanation)
- In-code templates: `docs/DPIA_TEMPLATE.md` (ICO Legitimate Interest Assessment), `docs/NOTICE_TEMPLATE.md` (GDPR Art. 14, CCPA §1798.100)

#### F2. Data Minimization Defaults

**Status:** PARTIALLY DONE — thumbnails off by default, OCR/embeddings togglable, retention enforced. Remaining:
- WebP compression for thumbnails (pending from A4)
- Batch zstd compression for JSON summaries

#### F3. Export, Deletion & Audit Log APIs

- `GET /api/sessions/export?from=DATE&to=DATE` → JSON/CSV export for data subject access requests
- `DELETE /api/sessions/{id}` → single-session deletion
- `DELETE /api/sessions?before=DATE` → batch deletion
- Audit log: track who exported/deleted what, when

#### F4. Per-Region Compliance Posture

- EU/UK GDPR: transparency notices, consent flows, DSR flows
- California CCPA/CPRA: notice, right to know/delete/opt-out
- Templates and checklist in `docs/`

---

### **Phase B: macOS Support** — Priority: MEDIUM

*Goal: Bring SurveilWin to macOS with native, high-performance capture.*

- New `Agent.Mac` project in Swift/SwiftUI
- ScreenCaptureKit with `SCContentSharingPicker` for user-consented capture
- Tesseract OCR and CLIP ONNX embeddings on macOS
- Local JSON summaries compatible with Windows output format

---

### **Phase C: Linux (Wayland) Support** — Priority: MEDIUM

*Goal: Bring SurveilWin to Linux with Wayland-native capture.*

- New `Agent.Linux` project in Rust/Go
- PipeWire + xdg-desktop-portal for permissioned capture
- CLI and headless mode with systemd integration
- Tesseract and ONNX Runtime on Linux

---

### **Phase D: Streaming Architecture & Scale** — Priority: LOW (deferred)

*Goal: Split single-process to enable scaling, cloud backend, and multi-device fleets.*

- Local gRPC split separating Agent from Backend/Runner
- Optional Kafka backbone for 50+ device fleets
- Storage upgrade: SQLite → PostgreSQL/ClickHouse + S3/MinIO
- New web Dashboard (React/Vue/Svelte) replacing WPF

---

## Implementation Priority Order

| Priority | Item | Status |
|----------|------|--------|
| 1 | ~~Multi-monitor cursor tracking~~ | ✅ SHIPPED |
| 2 | ~~App allow/deny lists + auto-pause~~ | ✅ SHIPPED |
| 3 | ~~Full trace mode~~ | ✅ SHIPPED |
| 4 | ~~Configuration system (appsettings.json)~~ | ✅ SHIPPED |
| 5 | ~~Data retention cleanup~~ | ✅ SHIPPED |
| 6 | ~~Adaptive FPS~~ | ✅ SHIPPED |
| 7 | ~~Dashboard UI overhaul~~ | ✅ SHIPPED |
| 8 | ~~Session management~~ | ✅ SHIPPED |
| 9 | WGC capture upgrade (A1) | NEXT |
| 10 | Zero-shot CLIP classifier (E1) | NEXT |
| 11 | Scene-change detection + ROI OCR (A4) | PLANNED |
| 12 | OCR keyword extraction + project mapping (E2) | PLANNED |
| 13 | WebP thumbnails + batch compression (A4/F2) | PLANNED |
| 14 | Onboarding notice + "What we collect" page (A5) | PLANNED |
| 15 | Privacy notice + DPIA templates (F1) | PLANNED |
| 16 | Export/delete/audit APIs (F3) | PLANNED |
| 17 | macOS agent (B) | PLANNED |
| 18 | Linux agent (C) | PLANNED |
| 19 | gRPC split + web dashboard (D) | DEFERRED |

---

## Performance & Cost-Efficiency Playbook

### Capture Layer

- **Adaptive FPS (✅ Shipped):**
  - Base: configurable via `CaptureFps` (default 1.0)
  - Doubles delay automatically when user is idle
  - Controlled by `AdaptiveFps` and `IdleThresholdSeconds` settings

- **Scene-Change Detection (Planned):**
  - Compute SSIM per frame vs. previous; if < 0.95, likely change
  - Burst to higher FPS on detected change; drop on static content
  - ~80% CPU/disk reduction on inactive sessions

- **Selective ROI OCR (Planned):**
  - Focus on title bar, top-left corner, content area
  - ~50% reduction in OCR latency

### Encoding & Storage

- **WebP Thumbnails (Planned):** ~30% smaller at similar quality vs JPEG
- **Batch zstd Compression (Planned):** 10 KB summary → ~2 KB (~80% reduction)
- **Retention (✅ Shipped):** Configurable auto-deletion for thumbnails and summaries

### Processing Layer

- **ONNX Quantization (Planned):** FP16/INT8 for ~2-4x faster inference
- **Batch Embeddings (Planned):** Accumulate 10 frames → batch embed + classify
- **Frame Pooling (Planned):** Reuse buffers to reduce GC pressure

---

## Security & Trust

### Code & Supply Chain

- Sign Windows executable (EV cert)
- Pin NuGet versions; audit quarterly with SBOM
- Tesseract official binaries or trusted repo sources
- ONNX model checksum verification on download

### Data Protection

- Encrypt `data/` folder on disk (BitLocker)
- Per-user data files with restrictive permissions
- TLS 1.2+ for any future network calls (gRPC, HTTP)

### Consent & Transparency

- **Dashboard Settings Panel (✅ Shipped):** Toggles for thumbnails, full-trace, OCR
- **Pause Button (✅ Shipped):** Stops capture immediately
- **Visual Indicators:** Yellow WGC border (pending A1), status dot in Dashboard (shipped)

---

## Success Metrics

### v0.2 (Current Release)

- ✅ Multi-monitor cursor tracking works across all connected displays
- ✅ App allow/deny policy correctly skips denied processes
- ✅ Full trace mode embeds per-frame data in summary JSON
- ✅ Configuration loaded from appsettings.json with all 16 settings
- ✅ Data retention auto-deletes old files on startup
- ✅ Dashboard shows live monitor, app, FPS, session info
- ✅ Build succeeds with 0 errors, 0 warnings on .NET 8

### Phase A Completion (Next)

- Stability: 99% uptime over 8-hour capture sessions
- Latency: First summary < 60 s after capture starts
- Resource: CPU < 5% on mid-range laptop; memory < 200 MB
- UX: WGC picker works; selected source displayed; capture border visible

### Phase E Completion

- Zero-shot classifier ≥ 85% top-1 accuracy on known activities
- Project mapping matches 90%+ of common identifiers in OCR
- Classification adds < 100 ms per batch

---

## References & Resources

### Windows Capture
- [Windows.Graphics.Capture API](https://learn.microsoft.com/en-us/windows/uwp/graphics-and-animation/windows-graphics-capture)
- [GitHub: Direct3D11CaptureFramePool examples](https://github.com/microsoft/windows-app-samples/tree/main/Samples/ScreenCaptureTests)

### OCR & Embeddings
- [Tesseract User Manual](https://tesseract-ocr.github.io/tessdoc/Installation.html)
- [CLIP: Learning Transferable Models](https://arxiv.org/abs/2103.00020)

### Streaming & Scale
- [Apache Kafka Documentation](https://kafka.apache.org/documentation/)
- [gRPC Framework](https://grpc.io)

### Privacy & Compliance
- [ICO: Employee Monitoring](https://ico.org.uk/for-organisations/employee-monitoring/)
- [GDPR Local: EDPB Guidelines & LIA](https://gdprlocal.com)

---

**Document Version:** 2.0
**Last Updated:** March 2026
**Status:** Engineering Roadmap (Partially Executed — v0.2 shipped)
