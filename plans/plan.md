# SurveilWin — Production Upgrade Plan
## Multi-Tenant, Multi-Role, Cloud-Backed Remote Employee Monitoring Platform

---

## Current State

SurveilWin is a **Windows MVP** with the following working components:
- `Agent.Win` — GDI screen capture, OCR (Tesseract), CLIP ONNX embeddings, activity & idle detection
- `Dashboard.Win` — WPF dark-theme UI for local monitoring
- `Runner` — Headless console runner
- `libs/` — Contracts, SlidingSummarizer, logging
- Outputs JSON summaries to **local disk** (`data/sessions/`)
- No authentication, no multi-user, no roles, no cloud storage
- Capture granularity: one summary every 30-60 seconds

---

## Target State

A **production-grade SaaS platform** for remote teams with:
1. Multi-organization, multi-role system (Owner → Admin → Manager → Employee)
2. Employee-side Windows agent (captures shift activity, uploads to cloud)
3. Central cloud backend (API + database)
4. Web dashboard with full RBAC
5. AI-powered daily work summaries (low cost)

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                        EMPLOYEE MACHINE (Windows)                   │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │  SurveilWin Agent (Modified Agent.Win)                       │   │
│  │  - Login with employee credentials (JWT)                     │   │
│  │  - Start Shift / End Shift buttons                           │   │
│  │  - Captures screen every 1-5s (adaptive FPS)                 │   │
│  │  - OCR text extraction per frame                             │   │
│  │  - App + window tracking                                     │   │
│  │  - Uploads activity data to backend API (not local files)    │   │
│  └──────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
                              │ HTTPS / REST API
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     BACKEND (ASP.NET Core 8)                        │
│  ┌────────────┐  ┌────────────────┐  ┌──────────────────────────┐  │
│  │  Auth API  │  │  Activity API  │  │  Summary/Reports API     │  │
│  │  JWT/RBAC  │  │  Frame upload  │  │  Daily summaries, export │  │
│  └────────────┘  └────────────────┘  └──────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │  Background Services                                         │   │
│  │  - Daily AI summarization job (low-cost LLM)                 │   │
│  │  - Retention cleanup job                                     │   │
│  │  - Shift auto-close job (if employee forgets to end shift)   │   │
│  └──────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
         │                              │
         ▼                              ▼
┌───────────────────┐        ┌──────────────────────┐
│  PostgreSQL DB    │        │  Blob Storage         │
│  - Users, Orgs   │        │  (Screenshots/Thumbs) │
│  - Shifts        │        │  Local disk / S3 /    │
│  - ActivityFrames│        │  Azure Blob / MinIO   │
│  - Summaries     │        └──────────────────────┘
└───────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────────────┐
│                   WEB DASHBOARD (React + TypeScript)                │
│  ┌──────────────────────────┐  ┌──────────────────────────────────┐ │
│  │  Admin/Manager View      │  │  Employee Self-Service View      │ │
│  │  - All employees list    │  │  - Own shift history             │ │
│  │  - Per-employee timeline │  │  - Own app usage                 │ │
│  │  - Activity breakdowns   │  │  - Daily AI summaries (own)      │ │
│  │  - AI daily summaries    │  │  - Shift start/end control       │ │
│  │  - Screenshots gallery   │  └──────────────────────────────────┘ │
│  └──────────────────────────┘                                       │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Roles

| Role | Scope | Capabilities |
|------|-------|-------------|
| **SuperAdmin** | Platform-wide | Create/manage organizations, platform config |
| **OrgAdmin** (Owner) | Organization | Manage all users, view all employee data, billing |
| **Manager** | Team subset | View activity of assigned employees only |
| **Employee** | Self only | View own shifts, activity, daily summaries |

---

## Implementation Phases

### Phase 1 — Backend API + Database + Auth
- README: `README_PHASE1_BACKEND.md`
- ASP.NET Core 8 Web API project
- PostgreSQL + Entity Framework Core
- JWT authentication, role-based claims
- Org/User management endpoints
- Activity data ingestion endpoints

### Phase 2 — Multi-Role RBAC System
- README: `README_PHASE2_ROLES.md`
- Role enforcement middleware
- Organization invite flow (email invite tokens)
- User profile management
- Shift management (start/end/auto-close)

### Phase 3 — Agent Upgrade (Cloud Upload Mode)
- README: `README_PHASE3_AGENT.md`
- Add login UI to Agent.Win (or new lightweight tray app)
- Shift start/end flow in agent
- Upload frames/summaries to backend API
- Store JWT token securely on employee machine
- Better capture accuracy (per-second granularity)

### Phase 4 — Improved Activity Tracking
- README: `README_PHASE4_TRACKING.md`
- Per-second app dwell time tracking
- App category classification (coding, browser, docs, communication, media)
- Productive vs non-productive app labeling
- URL domain extraction from browser titles
- Productivity score per hour/day
- Screenshot-based evidence with configurable retention

### Phase 5 — Web Dashboard
- README: `README_PHASE5_DASHBOARD.md`
- React + TypeScript + Vite + TailwindCSS
- Admin view: employee list, timelines, filters, reports
- Employee view: own data, shift history, daily summaries
- Charts: time-on-app pie chart, productivity timeline
- Screenshot gallery (admin only)
- Export: CSV/PDF reports

### Phase 6 — AI Daily Summaries (Low Cost)
- README: `README_PHASE6_AI.md`
- Pluggable AI provider (Ollama free, GPT-4o-mini, Gemini Flash)
- Daily batch job (runs end-of-day or midnight)
- Input: aggregated text data (NOT images) = very cheap tokens
- Output: Professional daily work summary per employee
- Cost estimate: ~$0.001/employee/day on GPT-4o-mini

---

## README Files Created

1. `README_PHASE1_BACKEND.md` — Backend API, DB schema, auth
2. `README_PHASE2_ROLES.md` — RBAC, roles, invite system
3. `README_PHASE3_AGENT.md` — Agent cloud upload, shift tracking
4. `README_PHASE4_TRACKING.md` — Improved monitoring accuracy
5. `README_PHASE5_DASHBOARD.md` — Web dashboard (React)
6. `README_PHASE6_AI.md` — Low-cost AI summaries

---

## Technology Stack

| Component | Technology | Reason |
|-----------|-----------|--------|
| Backend API | ASP.NET Core 8 | Same ecosystem as existing code |
| Database | PostgreSQL + EF Core 8 | Production-grade, free, JSONB support |
| Auth | JWT + Refresh Tokens | Stateless, mobile/agent friendly |
| Agent (Windows) | Modified Agent.Win (.NET 8) | Reuse existing capture pipeline |
| Web Dashboard | React 18 + TypeScript + Vite | Industry standard, fast |
| UI Components | TailwindCSS + shadcn/ui | Clean, accessible, dark theme |
| Charts | Recharts | Lightweight React charts |
| Blob Storage | Local filesystem → MinIO/S3 | Start local, upgrade to cloud |
| AI Provider | Ollama (local) / GPT-4o-mini | Zero cost (Ollama) or $0.001/day |
| Background Jobs | Hangfire / hosted services | .NET native, no extra infra |
| Containerization | Docker Compose | Easy self-hosting for startups |

---

## Cost Analysis for Startups

### Storage (10 employees, screenshots disabled)
- Activity JSON data: ~500KB/employee/day → 5MB/day → 1.5GB/year
- With PostgreSQL: fits on smallest VPS ($5/month)

### AI Summaries (GPT-4o-mini)
- Input: ~1500 tokens/employee/day (text summaries)
- Cost: $0.15/1M input tokens → $0.000225/employee/day
- 10 employees: ~$0.07/month on AI alone

### Screenshots (optional, admin-configurable)
- 1 screenshot/5min = 96/day/employee
- 50KB each (compressed WebP) → 4.8MB/employee/day
- 10 employees → 48MB/day → 1.4GB/month
- MinIO on same VPS: free storage

### Total Cost Estimate (10 employees)
| Item | Monthly Cost |
|------|-------------|
| VPS (2 CPU, 4GB RAM) | $10–20 |
| AI summaries (GPT-4o-mini) | $0.10 |
| Domain + SSL | $1 |
| **Total** | ~**$12–22/month** |

---

## Security Notes

- All data encrypted in transit (HTTPS/TLS 1.3)
- JWT secrets stored in environment variables (not appsettings)
- Passwords hashed with bcrypt/Argon2
- Agent stores JWT in Windows DPAPI-encrypted store
- Per-organization data isolation (row-level security)
- Audit log for all admin data access actions
- GDPR: Employee consent notice shown at agent first run
