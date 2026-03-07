# SurveilWin

A production-grade multi-tenant employee activity monitoring platform for Windows teams.

## Architecture

| Component | Stack | Description |
|-----------|-------|-------------|
| **Agent.Win** | .NET 8 WinForms | Background agent — captures app usage, OCR, screenshots |
| **Dashboard.Win** | .NET 8 WPF | Employee desktop UI — login, shift management |
| **SurveilWin.Api** | ASP.NET Core 8 | REST API — multi-tenant, JWT auth, EF Core + PostgreSQL |
| **SurveilWin.Web** | React 18 + Vite | Web dashboard — managers & admins |

## Quick Start (Docker)

### Prerequisites
- Docker Desktop
- 8GB RAM recommended (for Ollama AI)

### Run

```bash
git clone <repo>
cd surveil-win
docker compose up -d
```

Services will be available at:
- **Web Dashboard**: http://localhost:5173
- **API**: http://localhost:8080
- **API Docs (Swagger)**: http://localhost:8080/swagger
- **Ollama**: http://localhost:11434

### Pull AI Model (optional)

```bash
docker exec surveil-win-ollama-1 ollama pull llama3.2
```

## Development Setup

### Backend API

Requirements: .NET 8 SDK, PostgreSQL 16

```bash
# Set connection string
export ConnectionStrings__DefaultConnection="Host=localhost;Database=surveilwin;Username=postgres;Password=postgres"

cd apps/SurveilWin.Api
dotnet run
# API available at http://localhost:8080
# Swagger at http://localhost:8080/swagger
```

### Web Dashboard

Requirements: Node.js 20+

```bash
cd apps/SurveilWin.Web
npm install
npm run dev
# Available at http://localhost:5173
```

### Desktop Apps

Requirements: .NET 8 SDK + Windows

```bash
dotnet build SurveilWin.sln
```

## User Roles

| Role | Permissions |
|------|-------------|
| **SuperAdmin** | Full access across all orgs |
| **OrgAdmin** | Manage org, invite users, view all employee data |
| **Manager** | View assigned team activity |
| **Employee** | View own activity only |

## Features

- 🔐 JWT authentication with role-based access control
- 👥 Multi-tenant organization management
- 📊 Real-time activity tracking (app usage, category classification)
- 🤖 AI daily summaries (Ollama / OpenAI / Gemini)
- 📈 Productivity scoring with visual charts
- 🖥️ Native Windows agent with offline buffering
- 🐳 Docker Compose deployment with PostgreSQL + Ollama

## Configuration

### API (`apps/SurveilWin.Api/appsettings.json`)

| Key | Description | Default |
|-----|-------------|---------|
| `Jwt:Secret` | JWT signing key (min 32 chars) | change in prod |
| `Ai:Provider` | AI backend: `ollama`, `openai`, `gemini` | `ollama` |
| `Ai:OllamaUrl` | Ollama server URL | `http://ollama:11434` |
| `Email:Provider` | Email backend: `Log`, `Smtp` | `Log` |

### Desktop Agent (`appsettings.json`)

| Key | Description | Default |
|-----|-------------|---------|
| `ApiBaseUrl` | Backend API URL | `http://localhost:8080` |
| `CaptureFps` | Activity frame capture rate | `1.0` |

## License

See [LICENSE](LICENSE).
