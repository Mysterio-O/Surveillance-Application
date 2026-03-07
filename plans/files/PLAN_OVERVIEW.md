# SurveilWin Platform — Master Implementation Plan
## Production-Ready Remote Employee Monitoring System

---

## What This Is

This document is the **complete plan overview** for upgrading SurveilWin from a local Windows MVP to a production-ready, multi-tenant, cloud-backed platform for remote employee monitoring.

Six focused README files accompany this document — each one is a **standalone instruction document** for an AI agent to execute one phase of the build.

---

## README Documents

| File | Phase | What Gets Built |
|------|-------|----------------|
| `README_PHASE1_BACKEND.md` | Phase 1 | ASP.NET Core 8 API + PostgreSQL + JWT auth |
| `README_PHASE2_ROLES.md` | Phase 2 | Multi-role RBAC + invite system + org policy |
| `README_PHASE3_AGENT.md` | Phase 3 | Agent cloud upload + shift tracking + login |
| `README_PHASE4_TRACKING.md` | Phase 4 | App classification + productivity scoring |
| `README_PHASE5_DASHBOARD.md` | Phase 5 | React web dashboard with RBAC views |
| `README_PHASE6_AI.md` | Phase 6 | Low-cost AI daily summaries (Ollama/GPT/Gemini) |

---

## Execution Order

**Phases 1 → 2 → 3 → 4 must be done sequentially.**
**Phases 5 and 6 can be done in parallel after Phase 4.**

```
Phase 1: Backend API & DB       ← START HERE
    ↓
Phase 2: RBAC & Roles           ← depends on Phase 1
    ↓
Phase 3: Agent Upgrade          ← depends on Phase 2 (needs auth + shift APIs)
    ↓
Phase 4: Tracking Accuracy      ← depends on Phase 3 (needs frames in DB)
    ↙           ↘
Phase 5         Phase 6
Dashboard       AI Summaries
(parallel)      (parallel)
```

---

## Current Codebase (Do Not Break)

The existing SurveilWin codebase has:
- `apps/Agent.Win/` — Windows screen capture agent (7 services, .NET 8)
- `apps/Dashboard.Win/` — WPF monitoring UI (dark Catppuccin theme)
- `apps/Runner/` — Headless console runner
- `libs/Contracts/`, `libs/Processing/`, `libs/Utils/` — shared libraries
- `SurveilWin.sln` — Visual Studio solution

**Phases 1, 2, 5, 6** add NEW projects/apps to the solution without modifying existing code.
**Phases 3, 4** modify existing `Agent.Win` and `Dashboard.Win` projects.

---

## New Projects Added

| Project | Path | Type |
|---------|------|------|
| SurveilWin.Api | `apps/SurveilWin.Api/` | ASP.NET Core Web API |
| SurveilWin.Web | `apps/SurveilWin.Web/` | React 18 + TypeScript (Vite) |

---

## Key Technology Decisions

| Decision | Choice | Reason |
|----------|--------|--------|
| Backend framework | ASP.NET Core 8 | Matches existing .NET 8 ecosystem |
| Database | PostgreSQL + EF Core | Free, production-grade, JSONB support |
| Auth | JWT + Refresh Tokens | Stateless, works on agent + browser |
| Frontend | React + TypeScript | Industry standard, rich ecosystem |
| AI | Ollama (free) first | Zero cost, self-hosted, swap to cloud if needed |
| Storage | Local filesystem → S3/MinIO | Start simple, scale to blob storage |
| Deployment | Docker Compose | Easy self-hosting for startups |

---

## Final Architecture Summary

```
Employee Machine (Windows)
  └── SurveilWin Agent (modified Dashboard.Win)
        ├── Login with JWT
        ├── Start/End shift (calls API)
        ├── Captures screen every 1s
        ├── OCR text extraction
        ├── App category classification (local)
        └── Uploads frames to API (batch every 60s)
                  │ HTTPS
                  ▼
Backend API (ASP.NET Core 8)
  ├── Auth: JWT / RBAC
  ├── Org & User Management
  ├── Shift Tracking
  ├── Activity Ingestion (batch frame uploads)
  ├── Activity Aggregation (5-min windows)
  ├── AI Daily Summary Job (00:30 nightly)
  └── Reports & Export
          │          │
          ▼          ▼
   PostgreSQL    Screenshot
   (all data)    Storage
                 (local/S3)
                  │
                  ▼
Web Dashboard (React 18)
  ├── Admin View: all employees, timelines, charts, AI summaries
  └── Employee View: own data only
```

---

## Roles Summary

```
SuperAdmin → creates organizations (platform admin)
OrgAdmin   → manages all employees in their org (the "boss")
Manager    → views assigned employees only (team lead)
Employee   → views own data only (the worker)
```

---

## Cost (10 employees, self-hosted VPS)

- Hosting (VPS): ~$12–20/month
- AI summaries (Ollama): FREE
- AI summaries (GPT-4o-mini fallback): ~$0.40/month
- Total: **~$13–22/month for 10 employees**

---

## How to Use These README Files

Give each README to a capable AI coding agent (Claude, GPT-4, Copilot, etc.) with this prompt:

```
You are implementing Phase N of the SurveilWin platform.
Here is your detailed specification: [paste README content]

Repository root: surveil-win/
[Attach/reference the existing codebase as context]

Please implement everything described in this specification.
Run the build after each major addition. Fix any compilation errors before proceeding.
```

Each README is self-contained with:
- Exact file paths to create
- Database schemas (SQL)
- C# class definitions
- API endpoint specs
- Test checklists
- Acceptance criteria
