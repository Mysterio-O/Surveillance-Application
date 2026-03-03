# SurveilWin Product Roadmap

## Executive Summary

SurveilWin is evolving from a Windows-only MVP to a robust, cross-platform, privacy-aware, and scalable product. This roadmap outlines the engineering plan to evolve the current local, consent-first capture system into a multi-platform solution with streaming architecture, intelligent summarization, and full compliance readiness.

**Timeline:** 6–8 weeks (phased)  
**Target Platforms:** Windows (hardened), macOS, Linux (Wayland)  
**Architecture:** Single-process (local) → gRPC split → Kafka backbone (optional)

---

## Current Baseline

The MVP establishes:

- **Windows-only MVP** with:
  - Foreground-window screen sampling (GDI fallback) at ~1 fps
  - Local OCR (Tesseract) and optional embeddings (CLIP ONNX)
  - Sliding-window summaries (UI shows; Runner writes JSON + thumbnails)
  - No cloud dependencies; completely local; privacy-friendly defaults
- **Components:**
  - `Agent.Win`: Capture service, activity tracking, OCR, embeddings
  - `Dashboard.Win`: WPF UI for monitoring and configuration
  - `Runner`: Headless summarization engine
  - `Processing`: Sliding-window aggregation logic
  - `Contracts`: Shared domain models

---

## North-Star Product Goals

### 1. Consent-First Capture with System Pickers & Indicators

Users explicitly grant permissions; system UI provides visual feedback.

- **Windows:**
  - Windows.Graphics.Capture API with system picker
  - Yellow capture border (built-in OS feature) for visibility
  - Power-user controls: allow/deny lists, pause button
  
- **macOS:**
  - ScreenCaptureKit with system picker and per-window/app filtering
  - Built-in Screen Recording indicator
  - Graceful handling of DRM-protected content
  
- **Linux (Wayland):**
  - PipeWire + xdg-desktop-portal for permissioned capture
  - Portal dialogs per session; re-selection handling

### 2. Efficient Streaming & Near-Real-Time Analysis

- Local single-process pipeline initially
- gRPC split for separation of concerns
- Kafka backbone for scale and durability (multi-device fleets)

### 3. Explainable AI Summaries

Managers can audit activity with evidence:
- OCR text, app/window name, timestamps, thumbnails
- Optional embeddings/zero-shot labels (CLIP)
- Linked to projects and workflow context

### 4. Compliance-Ready by Design

- Transparency: "What is collected & why" pages
- Minimization: Embeddings/OCR only; thumbnails optional
- Retention policies: Thumbnails (7 days), summaries (90 days), configurable
- DPIA & Legitimate Interest Assessment templates
- Audit logs and access controls ready (GDPR, CCPA, UK ICO)

---

## Release Plan (Phased Roadmap)

### **Phase A: Windows Hardening** (1–2 weeks)

*Goal: Upgrade Windows capture to modern, consented API with better UX and performance.*

#### A1. Switch Capture to Windows.Graphics.Capture (WGC)

**Why:** System picker, high-performance frame capture, visible/auditable capture border.

**Changes:**

- **Agent.Win (CaptureService.cs)**
  - Re-add `Microsoft.Windows.SDK.BuildTools` and `Microsoft.Windows.CsWinRT` NuGet packages to `Agent.Win.csproj`
  - Replace GDI pipeline with WGC:
    - `GraphicsCapturePicker` → user selects display or window
    - `GraphicsCaptureSession` reading frames from `Direct3D11CaptureFramePool`
    - Convert Direct3D frames to CPU bitmap for OCR/embedding pipeline
  - Store selected `GraphicsCaptureItem` ID or metadata for Runner's headless mode

- **Dashboard.Win (MainWindow.xaml/xaml.cs)**
  - Add "Pick Source" button that invokes `GraphicsCapturePicker`
  - Display currently selected source (display name or window title)
  - Show that OS render capture border is active (inform user)

- **Runner (Program.cs)**
  - Add CLI options: `--monitor <index>` or `--window <title>` to select source headlessly
  - Load last-selected source from config if no CLI args provided

#### A2. App Allow/Deny Lists + Auto-Pause

**Purpose:** Respect user privacy by skipping capture on sensitive apps (email, banking, etc.).

**Changes:**

- **Contracts (Contracts.cs)**
  - Extend `CapturePolicy` to include `DenyListExes` (process names) and `DenyListTitlePatterns` (regex for window titles)

- **Agent.Win (CaptureService.cs)**
  - Query current foreground app (process name, window title)
  - Check against deny list; if matched, auto-pause and set `is_paused` flag in summary
  - Show banner in Dashboard: "Capture paused due to policy"

#### A3. Full Trace Mode (JSON)

**Purpose:** Enable detailed audit trail while respecting minimization by default.

**Changes:**

- **Contracts (Contracts.cs)**
  - Extend `Summary` schema to optionally include:
    - `ActiveApp`: process executable name
    - `WindowTitle`: current window title
    - `OcrText`: recognized text (gate behind config flag)
    - `TopKeywords`: extracted tokens/keywords
    - `EmbeddingLabel`: zero-shot category if embeddings enabled

- **Runner (Program.cs)**
  - Add config flag: `"enableFullTrace": false` (default)
  - When enabled, write extended JSON to `data/sessions/` with all fields above

#### A4. Performance & Stability

**Optimizations:**

- **Adaptive FPS:** Already in place; refine scene-change detection (SSIM comparison) to burst temporarily on high activity
- **Batch OCR:** Only run OCR on relevant ROIs (title bars, document areas) not entire frame
- **Thumbnails:** Store relative paths; compress with WebP (vs. JPEG) to reduce disk by ~30%
- **Frame pooling:** Reuse Direct3D buffers to reduce allocation overhead

#### A5. Consent & Privacy UX

- Add onboarding notice: "SurveilWin captures foreground activity. Pause anytime."
- "What is collected" page explaining:
  - App/window names, OCR text (optional), thumbnails (optional)
  - Where data is stored (local only, no cloud)
  - Retention periods (configurable)
  - How to pause or request deletion
- Ensure the yellow WGC border is visible (built-in; requires no extra work)

**Deliverables:**
- ✅ WGC capture pipeline (higher perf, system picker, visible border)
- ✅ Allow/deny list functionality
- ✅ Extended JSON logging
- ✅ UX improvements (onboarding, data transparency)

---

### **Phase B: macOS Support** (1–2 weeks)

*Goal: Bring SurveilWin to macOS with native, high-performance capture.*

#### B1. New Agent.Mac (Swift/SwiftUI)

**Project Structure:**
```
apps/Agent.Mac/
  ├── Sources/
  │   ├── Capture/
  │   │   ├── ScreenCaptureKitManager.swift
  │   │   └── FrameProcessor.swift
  │   ├── OCR/
  │   │   └── TesseractOCR.swift (wrapper)
  │   ├── Embedding/
  │   │   └── CLIPEmbedding.swift (ONNX wrapper)
  │   └── MainApp.swift
  ├── Package.swift
  └── Agent.Mac.xcodeproj
```

**Capture Selection:**
- `SCShareableContent.currentContent()` to enumerate available displays, windows, apps
- `SCContentSharingPicker` for user to select source (Apple's system picker)
- Respect macOS Screen Recording permission and entitlements

**Stream Setup:**
- `SCStreamConfiguration` to set FPS (target 30–60 fps, adaptive)
- Disable audio; capture video only
- `SCStream` with `SCStreamOutput` delegate to receive frames asynchronously

**Frame Processing:**
- Receive `CMSampleBuffer` → convert to `CGImage`
- Pass to Tesseract for OCR and ONNX Runtime for embeddings
- Same processing pipeline as Windows (reuse `Processing` logic or create Swift equivalent)

**Known Constraints:**
- macOS intentionally blocks many protected/DRM streams at GPU level (streaming services, password managers, etc.)
- Gracefully detect and log as "non-capturable" (don't attempt bypass)
- Show capture indicator (built-in macOS feature; no extra work)

#### B2. OCR & Embeddings on macOS

**Tesseract:**
- Distribute via Homebrew package or include as optional binary in DMG
- Same config as Windows (language packs, etc.)

**Embeddings:**
- Same CLIP ONNX model as Windows
- Wrap `onnxruntime` Objective-C or Swift bindings
- Store on-device; no network calls

#### B3. Dashboard & Configuration

- Minimal **Dashboard.Mac** (SwiftUI) or integrate into Agent.Mac UI
- Settings: source selection, pause, allow/deny lists, retention policies
- Alternatively, provide a web dashboard (see Phase D) for both platforms

**Deliverables:**
- ✅ Agent.Mac in Swift + ScreenCaptureKit
- ✅ Tesseract OCR and ONNX embeddings on macOS
- ✅ System picker and permission handling
- ✅ Local JSON summaries compatible with Windows Runner output

---

### **Phase C: Linux (Wayland) Support** (1–2 weeks)

*Goal: Bring SurveilWin to Linux with Wayland-native capture.*

#### C1. New Agent.Linux (Rust/Go or native C++)

**Architecture:**

```
apps/Agent.Linux/
  ├── src/
  │   ├── capture/
  │   │   ├── pipewire.rs (PipeWire streaming)
  │   │   └── portal.rs (xdg-desktop-portal integration)
  │   ├── ocr/
  │   │   └── tesseract.rs (wrapper)
  │   ├── embedding/
  │   │   └── onnx.rs (ONNX Runtime)
  │   └── main.rs
  ├── Cargo.toml
  └── build.rs
```

**Capture Flow:**

1. **xdg-desktop-portal ScreenCast:**
   - Show system portal picker (compositor handles UI)
   - User selects display or window
   - Obtain PipeWire node ID

2. **PipeWire Streaming:**
   - Connect to PipeWire server
   - Subscribe to selected node (video output)
   - Receive frames as SPA buffers (raw pixel data)
   - Convert to bitmap for OCR/embedding

3. **Graceful Handling:**
   - Some compositors re-authenticate per session; cache "last selection" if allowed
   - Handle re-selection prompts as normal (document in UX)
   - Log errors if capture fails (X11 fallback not recommended; Wayland is mandatory for long-term)

#### C2. OCR & Embeddings on Linux

**Tesseract:**
- Available on all major distros (apt-get, dnf, pacman, etc.)
- Provide build instructions in README

**ONNX Runtime:**
- Pre-built for Linux; fetch via package manager or build from source
- Same CLIP model as Windows/macOS

#### C3. CLI & Headless Mode

- Provide CLI args like Windows: `--monitor 0 --window <pattern>`
- Integrate with systemd for user-level service (optional, Phase D)
- Write summaries to `data/sessions/` in same format as Windows/macOS

**Deliverables:**
- ✅ Agent.Linux via PipeWire + xdg-desktop-portal
- ✅ Tesseract and ONNX on Linux
- ✅ Compatible JSON output
- ✅ Documentation: systemd service setup (optional)

---

### **Phase D: Streaming Architecture & Scale** (2–3 weeks)

*Goal: Split single-process to enable scaling, cloud backend, and multi-device fleets.*

#### D1. Local gRPC Split

**Architecture:**

```
Current (Phase A–C):
┌─────────────────────────┐
│ Agent (Windows/Mac/Lin) │
│ - Capture              │
│ - OCR/Embeddings       │
│ - Summarization        │
└─────────────────────────┘
           ↓
     data/sessions/*.json

New (Phase D):
┌────────────────┐      gRPC      ┌──────────────────┐
│ Agent          │ ──────────────→ │ Backend/Runner   │
│ - Capture      │   micro-batch   │ - Aggregation    │
│ - OCR/Embed    │   every 10–30s  │ - Summarization  │
└────────────────┘                 │ - Storage        │
                                   └──────────────────┘
                                          ↓
                                   data/sessions/
                                   + API responses
```

**gRPC Service Definition:**

```protobuf
service SummarySink {
  rpc SendActivity(ActivityBatch) returns (Ack) {}
}

message ActivityBatch {
  string session_id = 1;
  int64 timestamp_ms = 2;
  string app = 3;
  string window_title = 4;
  string ocr_text = 5;
  bytes thumbnail = 6;
  repeated float embedding = 7;
  bool is_idle = 8;
}
```

**Agent Changes:**
- Collect frames, OCR, embeddings in memory
- Batch every 10–30 s
- Send to Backend via gRPC
- Fall back to local JSON if Backend unavailable

**Backend (Runner refactor):**
- Listen on gRPC port (e.g., 50051)
- Aggregate incoming batches
- Apply sliding-window summarization
- Write to `data/sessions/` and optional API responses

#### D2. (Optional) Kafka for Multi-Device Scale

**When needed:** Fleet with 50+ endpoints

**Setup:**
- Create Kafka topics: `surveil-activity-{user_id}` or `surveil-{device_id}`
- Agent → Kafka producer (TLS, auth)
- Backend consumer(s) read and aggregate
- Benefits: durability, multi-consumer patterns (logging, alerting, analytics), replay-ability

**Not required for initial Phases A–C; plan for later expansion.**

#### D3. Storage Upgrade

**Development/Small Deployment:**
- Keep local `data/sessions/` JSON + thumbnails (WebP)
- Add SQLite for metrics and metadata

**Production/Multi-Tenant:**
- PostgreSQL or ClickHouse for summaries and metrics
- S3 or MinIO for thumbnails with TTL lifecycle (e.g., delete after 7 days)
- Kafka for event stream (durability)

#### D4. Web Dashboard

**Transition WPF to Web:**
- Agent remains platform-specific (Windows/macOS/Linux)
- New **Dashboard.Web** (React/Vue/Svelte) consuming Backend APIs
- Backend provides REST/GraphQL endpoints:
  - `GET /sessions` → list summaries
  - `GET /sessions/{id}` → full trace with OCR, thumbnails, timestamps
  - `DELETE /sessions/{id}` → user-initiated deletion
- RBAC by API key or per-tenant auth
- Audit logging (who accessed what, when)

**Deliverables:**
- ✅ gRPC proto definitions and service
- ✅ Agent micro-batching logic
- ✅ Backend aggregation refactor
- ✅ Web Dashboard (minimal: list, view, delete)
- ⏳ Kafka setup (defer to Phase D+1 if budget tight)

---

### **Phase E: Intelligence & Labeling** (1–2 weeks initial)

*Goal: Add zero-shot labeling and project mapping for richer, auditable summaries.*

#### E1. Zero-Shot Labels via CLIP

**Concept:**
Use the same CLIP embedding model to classify activity:

```python
prompt_categories = [
    "code editor",
    "IDE",
    "slides presentation",
    "spreadsheet",
    "browser window",
    "email or messaging",
    "video or social media",
    "documentation",
    "meetings or video calls",
    "terminal or command line"
]

# Embed each prompt + compare to screenshot embedding
# Return top N categories with scores
```

**Implementation:**
- Compute embedding for each prompt once (cache)
- For each frame, compute embedding
- Cosine similarity to prompts → top 3 categories with confidence
- Store in Summary: `"activities": [{"category": "code editor", "confidence": 0.92}]`

**Benefits:**
- More context than just app name (e.g., Firefox → "code editor" or "browser")
- Explainable (manager can see why system labeled activity)
- Works across platforms (model is same ONNX binary)

#### E2. OCR Keyword Extraction & Project Mapping

**Flow:**

1. Extract tokens from OCR: JIRA-123, git repo names, file paths
2. Maintain lightweight project dictionary (config):
   ```json
   {
     "projects": {
       "ProjectA": {
         "patterns": ["JIRA-A-\\d+", "repo-a/", "path/to/projectA"]
       },
       "ProjectB": {
         "patterns": ["JIRA-B-\\d+", "path/to/projectB"]
       }
     }
   }
   ```
3. Match OCR text → tag summary with project
4. Store in Summary: `"project": "ProjectA"`, `"identifiers": ["JIRA-A-123"]`

**Benefits:**
- Attach "where work happened" to activity
- Managers can see time-per-project without relying on title parsing
- Explainable (matched identifiers shown in summary)

#### E3. Summarization Model (Extensible)

**Phase E:** Keep rule-first logic (keyword frequency, app dwell time, etc.)

**Future:** Plug-in interface for local LLM (e.g., Ollama, llama.cpp):
- Optional; off by default (performance/privacy tradeoff)
- Always include evidence: timestamps, OCR snippets, thumbnails, screenshots
- Summaries remain auditable

**Deliverables:**
- ✅ Zero-shot CLIP classify
- ✅ Project keyword mapping
- ✅ Extensible summarization (rule-based + LLM plug-in interface)

---

### **Phase F: Privacy, Compliance & Policy** (parallel work, ongoing)

*Goal: Ship compliance-ready by default; support EU, UK, and California regulations.*

#### F1. Privacy Notice & DPIA Templates

**Dashboard Page: "What We Collect"**

```
SurveilWin captures:
- ✓ App name (process) and window title
- ✓ OCR text (if enabled) to understand activity
- ✓ Thumbnails (if enabled) for visual context
- ✓ Embeddings (if enabled) for category labeling

NOT captured:
- Audio, video, keyboard input, clipboard
- Password fields or sensitive form data (browser auto-filters)

Storage:
- All data stored locally on this device; no cloud sync
- Thumbnails retained for 7 days; summaries for 90 days (configurable)
- You can pause or delete at any time

Legal notices:
- [Privacy Policy link]
- [Company Data Processing Addendum for EU customers]
```

**In-Code Templates:**
- `docs/DPIA_TEMPLATE.md`: Legitimate Interest Assessment for employee monitoring (ICO guidance)
- `docs/NOTICE_TEMPLATE.md`: Employee notice (GDPR Art. 14, CCPA §1798.100)

#### F2. Data Minimization Defaults

**Default Configuration:**

```json
{
  "capture": {
    "fps": 1.0,
    "enabled": true
  },
  "ocr": {
    "enabled": true
  },
  "embeddings": {
    "enabled": true
  },
  "thumbnails": {
    "enabled": false,  // User must opt-in
    "compression": "webp"
  },
  "retention": {
    "thumbnails_days": 7,
    "summaries_days": 90
  }
}
```

- Thumbnails **off** by default (opt-in only)
- OCR and embeddings on (low-risk local processing)
- Automatic cleanup after retention period

#### F3. Access, Deletion & Compliance APIs

**Dashboard Features:**

- **Export:**
  - `GET /api/sessions/export?from=DATE&to=DATE` → JSON/CSV
  - Include: timestamps, app, OCR (if enabled), project/category labels
  - For staff data subject access requests (GDPR Art. 15, CCPA §1798.100)

- **Delete:**
  - `DELETE /api/sessions/{id}` → Remove specific session
  - `DELETE /api/sessions?before=DATE` → Batch delete older sessions
  - For erasure requests (GDPR Art. 17, CCPA §1798.105)

- **Audit Log:**
  - Track who exported/deleted what, when
  - Helps HR prove compliance to regulators

#### F4. Per-Region Posture

**EU/UK (GDPR):**
- Transparency notices (what, why, how long)
- Consent for optional features (thumbnails, summary model)
- Data Subject Access/Deletion/Portability flows (APIs above)
- DPA/Processor agreement template

**California (CCPA/CPRA):**
- Notice of privacy practices
- Right to know, delete, opt-out (same APIs as GDPR)
- No "sale" of personal information (local-only by default)

**Guidance References:**
- [ICO: "Employee monitoring – is it right for your business?"](https://ico.org.uk)
- [EDPB Guidelines 05/2020 on consent](https://gdprlocal.com)

**Deliverables:**
- ✅ Privacy notice page in Dashboard
- ✅ DPIA/LIA templates in `docs/`
- ✅ Export/delete APIs
- ✅ Audit log table
- ✅ Per-region compliance checklist

---

## Concrete Change List (File-by-File)

### Windows Hardening (Phase A)

| File | Change | Owner |
|------|--------|-------|
| `apps/Agent.Win/Agent.Win.csproj` | Re-add `Microsoft.Windows.SDK.BuildTools`, `Microsoft.Windows.CsWinRT` | Backend |
| `apps/Agent.Win/Services/CaptureService.cs` | Replace GDI → WGC pipeline; add allow/deny check; store selected item | Backend |
| `apps/Dashboard.Win/Views/MainWindow.xaml` | Add "Pick Source" button; show current selection | Frontend |
| `apps/Dashboard.Win/Views/MainWindow.xaml.cs` | Event handler for picker; update UI with selection | Frontend |
| `apps/Runner/Program.cs` | Add `--monitor` / `--window` CLI args; headless source selection | Backend |
| `libs/Contracts/Contracts.cs` | Extend `Summary` (ActiveApp, WindowTitle, OcrText, TopKeywords, EmbeddingLabel) | Backend |
| `libs/Contracts/Contracts.cs` | Add `CapturePolicy` (DenyListExes, DenyListTitlePatterns) | Backend |
| `apps/Dashboard.Win/Views/OnboardingPage.xaml` | (New) Privacy notice + "What we collect" | Frontend |

### macOS Support (Phase B)

| File | Change | Owner |
|------|--------|-------|
| `apps/Agent.Mac/` | (New project in Swift/SwiftUI) | Backend |
| `apps/Agent.Mac/Sources/Capture/ScreenCaptureKitManager.swift` | (New) SCK picker, SCStream, frame processing | Backend |
| `apps/Agent.Mac/Sources/OCR/TesseractOCR.swift` | (New) Tesseract wrapper | Backend |
| `apps/Agent.Mac/Sources/Embedding/CLIPEmbedding.swift` | (New) ONNX Runtime for macOS | Backend |
| `apps/Agent.Mac/Agent.Mac.xcodeproj` | (New Xcode project + entitlements) | Build |

### Linux Support (Phase C)

| File | Change | Owner |
|------|--------|-------|
| `apps/Agent.Linux/` | (New project in Rust/Go) | Backend |
| `apps/Agent.Linux/src/capture/portal.rs` | (New) xdg-desktop-portal picker integration | Backend |
| `apps/Agent.Linux/src/capture/pipewire.rs` | (New) PipeWire streaming, frame decode | Backend |
| `apps/Agent.Linux/src/ocr/tesseract.rs` | (New) Tesseract wrapper | Backend |
| `apps/Agent.Linux/src/embedding/onnx.rs` | (New) ONNX Runtime for Linux | Backend |

### Streaming & Scale (Phase D)

| File | Change | Owner |
|------|--------|-------|
| `protos/summary.proto` | (New) gRPC service + message definitions | Backend |
| `apps/Agent.Win/Services/` | Add gRPC client; micro-batch logic | Backend |
| `apps/Agent.Mac/Sources/` | Add gRPC client; micro-batch logic | Backend |
| `apps/Agent.Linux/src/` | Add gRPC client; micro-batch logic | Backend |
| `apps/Runner/Program.cs` | Refactor as gRPC server; consumer logic | Backend |
| `libs/Processing/SlidingSummarizer.cs` | (Refactor) Use aggregated batches, not files | Backend |
| `apps/Dashboard.Web/` | (New project) React/Vue; API consumers | Frontend |
| `backends/Server/` | (New) ASP.NET/Node.js API server | Backend |

### Intelligence & Labeling (Phase E)

| File | Change | Owner |
|------|--------|-------|
| `libs/Processing/CLIPClassifier.cs` | (New) Zero-shot prompt embedding + cosine similarity | ML/Backend |
| `libs/Processing/SlidingSummarizer.cs` | Integrate CLIP categories into Summary | Backend |
| `libs/Processing/ProjectMapper.cs` | (New) OCR token extraction + regex dictionary matching | Backend |
| `libs/Contracts/Contracts.cs` | Extend `Summary`: add `Activities[]`, `Project`, `Identifiers[]` | Backend |

### Privacy & Compliance (Phase F)

| File | Change | Owner |
|------|--------|-------|
| `apps/Dashboard.Win/Views/ComplianceNotice.xaml` | (New) "What we collect" + retention info | Frontend |
| `apps/Dashboard.Web/pages/ComplianceNotice.tsx` | (New) same for web | Frontend |
| `backends/Server/Controllers/DataController.cs` | (New) Export, delete, audit log APIs | Backend |
| `docs/DPIA_TEMPLATE.md` | (New) ICO Legitimate Interest Assessment template | Compliance |
| `docs/NOTICE_TEMPLATE.md` | (New) Employee notice template (GDPR/CCPA) | Compliance |
| `docs/PRIVACY_POLICY.md` | (New) Master privacy policy | Compliance |
| `README.md` | Update with compliance & feature overview | DevRel |

---

## Performance & Cost-Efficiency Playbook

### Capture Layer

- **Adaptive FPS (Phase A+):**
  - Base: 0.5–1 fps for idle
  - Burst to 5–10 fps on detected app switch or scene change (SSIM > threshold)
  - Reduces CPU/disk by ~80% on inactive sessions

- **Scene-Change Detection:**
  - Compute SSIM per frame vs. previous; if < 0.95, likely change
  - Alternatively: hash + mismatch check (faster, less accurate)

- **Selective ROI OCR:**
  - Don't OCR entire frame; focus on:
    - Title bar (window/tab name)
    - Top-left corner (IDE class indicators)
    - Content area (detect document type, skim text)
  - ~50% reduction in OCR latency

### Encoding & Storage

- **Thumbnail Compression:**
  - Use WebP (lossy) instead of JPEG; ~30% smaller at similar quality
  - FPS 1 fps × 3600 s = 3600 frames/hour → ~100 MB/hour WebP at 640×480 vs. 150 MB JPEG
  - TTL deletion (7 days default) → ~0.7 GB for typical week

- **Batch Compression:**
  - Use zstd (level 3) for JSON summaries
  - 10 KB summary → ~2 KB zstd → 80% reduction

### Processing Layer

- **ONNX Quantization:**
  - Convert CLIP model to FP16 (half precision) or INT8 (post-training quantization)
  - ~2–4x faster inference, ~50% smaller model
  - Minimal accuracy loss for classification

- **Batch Embeddings:**
  - Accumulate 10 frames → batch embed + classify
  - Amortizes startup cost; 1 embedding per 10 frames instead of 1 per frame

- **Frame Pooling:**
  - Reuse Direct3D/Metal buffers instead of allocating new ones each frame
  - Reduce GC pressure; more stable FPS

### Distribution (Phase D+)

- **gRPC Micro-Batches:**
  - Send 10–30 s of activity (not per-frame)
  - Typical: 1 KB × 30 frames = 30 KB → compress to 6 KB → send every 30 s = 200 B/s ≈ 17 MB/day
  - Much lower than per-frame streaming

- **Kafka (Optional for Fleet):**
  - Durable topic with retention (e.g., 7 days)
  - Parallelizable consumers (one per device/user)
  - Replay-ability for debugging/audits

### Cloud Inference (Future)

- **Defer expensive tagging to backend:**
  - Skip embeddings on-device if CPU budget < 5%
  - Backend batches embeddings across users (cost amortized)
  - Or: selective embedding (every Nth frame) on device, full coverage at backend

---

## Security & Trust

### Code & Supply Chain

- **Signing:**
  - Sign Windows executable (EV cert)
  - Notarize macOS app (Apple Developer Program)
  - GPG sign Linux releases
  - Publish hashes (SHA-256) for verification

- **Dependencies:**
  - Pin NuGet, npm, Cargo versions; audit quarterly with SBOM
  - Tesseract official binaries or trusted repo sources
  - ONNX model checksum verification on download

### Network Security

- **Local Services (Phase D):**
  - gRPC over TLS (self-signed cert acceptable for localhost)
  - mTLS if multi-device (client cert auth)

- **API Authentication:**
  - Bearer token (short-lived JWTs) or API key per tenant
  - Rotate credentials monthly

### Data Protection

- **At Rest:**
  - Encrypt `data/` folder on disk (BitLocker, FileVault, dm-crypt)
  - Per-user data files with restrictive permissions (0600)

- **In Transit:**
  - TLS 1.2+ for any network calls (gRPC, HTTP)
  - Disable HTTP; HTTPS only

### Consent & Transparency

- **Visual Indicators (Platform Built-In):**
  - Windows: Yellow WGC border (no extra code needed)
  - macOS: Capture indicator in menu bar (built-in)
  - Linux: Portal dialog (system-level; no extra code)

- **"Pause" Button:**
  - Prominent and always available
  - Stops capture immediately; no buffering
  - Confirmed in UI ("Paused until you click Resume")

- **Settings → "What we collect":**
  - Plain-language explanation of each feature
  - Toggle to turn off OCR, embeddings, thumbnails
  - Show retention periods and deletion policy

---

## Success Metrics (Per Phase)

### Phase A: Windows Hardening
- ✅ **Stability:** 99% uptime over 8-hour capture sessions (zero stuck frames)
- ✅ **Latency:** First summary generated < 60 s after capture starts
- ✅ **Resource Use:** CPU < 5% on mid-range laptop (i5, 8 GB RAM); memory < 200 MB
- ✅ **UX:** Picker works; selected source displayed; pause button responds < 100 ms

### Phase B & C: macOS & Linux
- ✅ **Parity:** Same latency, resource, and UX metrics as Windows
- ✅ **Picker:** System pickers work end-to-end
- ✅ **Permissions:** Respect Screen Recording (macOS), Portal authorization (Linux)
- ✅ **Output:** JSON summaries compatible with Windows format

### Phase D: Streaming & Scale
- ✅ **gRPC Latency:** p95 < 200 ms on localhost (typical batch size 1 KB)
- ✅ **Kafka Consumer Lag:** < 2 s (if Kafka deployed)
- ✅ **Backward Compat:** Single-process mode still works for local deployments

### Phase E: Intelligence
- ✅ **Embedding Accuracy:** Zero-shot classifier achieves ≥ 85% top-1 accuracy on known activities (code, browser, messaging, etc.)
- ✅ **Project Mapping:** Regex patterns match 90%+ of common identifiers in OCR (JIRA, file paths, repos)
- ✅ **Latency Neutral:** Classification adds < 100 ms per batch

### Phase F: Compliance
- ✅ **Coverage:** Privacy notice page, DPIA/notice templates, export/delete APIs all shipped
- ✅ **Audit Trail:** All data access (read, delete, export) logged with timestamp + user
- ✅ **Retention:** Thumbnails auto-deleted after 7 days; summaries after 90 days
- ✅ **Documentation:** README includes compliance checklist for EU, UK, CA

---

## Quick Start: What Can Be Delivered Immediately

If you confirm, these can ship **within 1–2 weeks**:

### 1. **Windows.Graphics.Capture Upgrade (Phase A1 + A2)**
   - Switch from GDI to WGC (higher perf, system picker, visible border)
   - Add allow/deny list + auto-pause on sensitive apps
   - **Deliverable:** Agent.Win with WGC capture, "Pick Source" button, pause banner
   - **Impact:** Dramatic UX improvement; user explicitly consents to what's captured

### 2. **Full Trace Mode (Phase A3)**
   - Extend Summary schema to include ProcessName, WindowTitle, OcrText, TopKeywords, EmbeddingLabel
   - Gate behind `"enableFullTrace": false` config
   - **Deliverable:** Detailed JSON logs for audit/compliance teams
   - **Impact:** Transparency; managers can see exactly what was captured

### 3. **Zero-Shot Labeling (Phase E1)**
   - Use existing CLIP ONNX model to classify screenshots (code editor, browser, messaging, etc.)
   - Compare embeddings to 10 fixed prompts; store top 3 categories + confidence
   - **Deliverable:** Enhanced Summary with `"activities": [{"category": "code editor", "confidence": 0.92}]`
   - **Impact:** Richer summaries; more explainable than app titles alone

### How to Proceed

1. **Confirm** that Windows hardening + full trace + zero-shot labeling align with roadmap
2. Then I will:
   - Update `Agent.Win.csproj` with WGC packages
   - Refactor `CaptureService.cs` (WGC pipeline + allow/deny checks)
   - Extend `Contracts.cs` and `Summary` schema
   - Add CLIP zero-shot classifier to `Processing.cs`
   - Create "What we collect" UI page
3. Test on Windows (no SDK install issues needed; uses modern APIs)
4. Commit & document

**Then continue to Phase B (macOS) after Windows is stable.**

---

## References & Resources

### Windows Capture

- [Windows.Graphics.Capture API](https://learn.microsoft.com/en-us/windows/uwp/graphics-and-animation/windows-graphics-capture)
- [Swyshare: Audio/Screen Capture in C# .NET without WinRT](https://swyshare.com/blog/2014/08/audio-screen-capture-in-c-net-without-winrt/)
- [GitHub: Direct3D11CaptureFramePool examples](https://github.com/microsoft/windows-app-samples/tree/main/Samples/ScreenCaptureTests)

### macOS Capture

- [ScreenCaptureKit Overview](https://developer.apple.com/documentation/screencapturekit)
- [OBS macOS ScreenCaptureKit Integration](https://obsproject.com)
- [WWDC 2023: Introducing ScreenCaptureKit](https://developer.apple.com/videos/play/wwdc2023/10044/)

### Linux / Wayland Capture

- [PipeWire Documentation](https://docs.pipewire.org)
- [xdg-desktop-portal Specification](https://flatpak.github.io/xdg-desktop-portal/)
- [Fisher Phillips: Screening in a Screened Out World (PipeWire)](https://fisherphillips.com)
- [Void Linux PipeWire Guide](https://docs.voidlinux.org)

### OCR & Embeddings

- [Tesseract User Manual](https://tesseract-ocr.github.io/tessdoc/Installation.html)
- [CLIP: Learning Transferable Models for Computer Vision](https://arxiv.org/abs/2103.00020)
- [Rust ONNX Runtime](https://docs.rs/ort/latest/ort/)

### Streaming & Scale

- [Apache Kafka Documentation](https://kafka.apache.org/documentation/)
- [gRPC: A High Performance RPC Framework](https://grpc.io)
- [Monitask: Time Tracking Architecture](https://monitask.com/blog)

### Privacy & Compliance

- [ICO: Employee Monitoring – Is It Right for Your Business?](https://ico.org.uk/for-organisations/employee-monitoring/)
- [GDPR Local: EDPB Guidelines & LIA](https://gdprlocal.com)
- [EDPB Guidelines 05/2020: Consent under GDPR](https://ec.europa.eu/info/law/law-topic/data-protection/data-protection-eu-reform/regulation-text_en)
- [UK Information Commissioner's Office: GDPR & Monitoring](https://ico.org.uk)

---

**Document Version:** 1.0  
**Last Updated:** March 2026  
**Status:** Engineering Roadmap (Ready to Execute)
