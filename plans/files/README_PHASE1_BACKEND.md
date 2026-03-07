# Phase 1 — Backend API, Database & Authentication
## SurveilWin Production Platform

---

## Overview

This document instructs an AI agent to build the **ASP.NET Core 8 Web API backend** for the SurveilWin remote employee monitoring platform. This is the foundational layer — all other phases depend on this.

**Deliverable:** A fully functional REST API with JWT authentication, multi-tenant organization support, and a PostgreSQL database schema.

---

## Existing Codebase Context

The existing project lives at the repository root `surveil-win/`. It contains:
- `apps/Agent.Win/` — Windows capture agent (class library, .NET 8)
- `apps/Dashboard.Win/` — WPF app (.NET 8, net8.0-windows)
- `apps/Runner/` — Console headless runner (.NET 8)
- `libs/Contracts/Contracts.cs` — Shared data types (`FrameFeature`, `Summary`, `AppConfig`)
- `libs/Processing/SlidingSummarizer.cs` — Sliding-window activity aggregator
- `SurveilWin.sln` — Visual Studio solution

**Do NOT modify existing projects.** Add new projects alongside them.

---

## What to Build

### 1. Create a New ASP.NET Core Web API Project

**Project path:** `apps/SurveilWin.Api/SurveilWin.Api.csproj`

**Add to solution:**
```bash
dotnet new webapi -n SurveilWin.Api -o apps/SurveilWin.Api --framework net8.0
dotnet sln SurveilWin.sln add apps/SurveilWin.Api/SurveilWin.Api.csproj
```

**NuGet packages to install:**
```bash
dotnet add apps/SurveilWin.Api package Microsoft.EntityFrameworkCore --version 8.0.*
dotnet add apps/SurveilWin.Api package Microsoft.EntityFrameworkCore.Design --version 8.0.*
dotnet add apps/SurveilWin.Api package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.*
dotnet add apps/SurveilWin.Api package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.*
dotnet add apps/SurveilWin.Api package BCrypt.Net-Next --version 4.0.3
dotnet add apps/SurveilWin.Api package Swashbuckle.AspNetCore --version 6.5.*
dotnet add apps/SurveilWin.Api package Microsoft.AspNetCore.Authorization --version 8.0.*
```

---

## Database Schema

### PostgreSQL Tables

Create the following Entity Framework Core entity models in `apps/SurveilWin.Api/Data/Entities/`.

#### Organizations
```sql
CREATE TABLE organizations (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name        VARCHAR(200) NOT NULL,
    slug        VARCHAR(100) NOT NULL UNIQUE,  -- used in subdomain/URL
    plan        VARCHAR(50) NOT NULL DEFAULT 'free', -- free, starter, pro
    is_active   BOOLEAN NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

#### Users
```sql
CREATE TABLE users (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
    email           VARCHAR(320) NOT NULL,
    password_hash   VARCHAR(500) NOT NULL,
    first_name      VARCHAR(100) NOT NULL,
    last_name       VARCHAR(100) NOT NULL,
    role            VARCHAR(50) NOT NULL,  -- 'SuperAdmin','OrgAdmin','Manager','Employee'
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    invited_by      UUID REFERENCES users(id),
    invite_token    VARCHAR(200),          -- null once accepted
    invite_expires  TIMESTAMPTZ,
    last_login_at   TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (organization_id, email)
);
CREATE INDEX idx_users_org ON users(organization_id);
CREATE INDEX idx_users_email ON users(email);
```

#### Manager-Employee Assignments
```sql
CREATE TABLE manager_assignments (
    manager_id  UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    employee_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (manager_id, employee_id)
);
```

#### Shifts
```sql
CREATE TABLE shifts (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id     UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    organization_id UUID NOT NULL REFERENCES organizations(id),
    date            DATE NOT NULL,
    started_at      TIMESTAMPTZ NOT NULL,
    ended_at        TIMESTAMPTZ,           -- NULL if shift still active
    expected_hours  DECIMAL(4,2) DEFAULT 8.0,
    actual_hours    DECIMAL(4,2),          -- calculated on shift end
    status          VARCHAR(20) NOT NULL DEFAULT 'active', -- active, completed, auto_closed
    agent_version   VARCHAR(50),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_shifts_employee ON shifts(employee_id);
CREATE INDEX idx_shifts_date ON shifts(date);
```

#### Activity Sessions (one per shift connection)
```sql
CREATE TABLE activity_sessions (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shift_id        UUID NOT NULL REFERENCES shifts(id) ON DELETE CASCADE,
    employee_id     UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    organization_id UUID NOT NULL REFERENCES organizations(id),
    session_key     VARCHAR(100) NOT NULL UNIQUE, -- from agent (session_YYYYMMDD_HHmmss)
    started_at      TIMESTAMPTZ NOT NULL,
    ended_at        TIMESTAMPTZ,
    total_frames    INTEGER NOT NULL DEFAULT 0,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

#### Activity Frames (core telemetry data)
```sql
CREATE TABLE activity_frames (
    id              BIGSERIAL PRIMARY KEY,
    session_id      UUID NOT NULL REFERENCES activity_sessions(id) ON DELETE CASCADE,
    employee_id     UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    organization_id UUID NOT NULL REFERENCES organizations(id),
    captured_at     TIMESTAMPTZ NOT NULL,
    active_app      VARCHAR(260) NOT NULL,     -- process name
    window_title    VARCHAR(1000) NOT NULL,    -- foreground window title
    app_category    VARCHAR(50),               -- coding, browser, docs, communication, media, idle, other
    is_idle         BOOLEAN NOT NULL DEFAULT FALSE,
    ocr_text        TEXT,
    monitor_index   SMALLINT,
    cursor_x        INTEGER,
    cursor_y        INTEGER,
    thumbnail_path  VARCHAR(500),             -- relative path to screenshot file
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_frames_session ON activity_frames(session_id);
CREATE INDEX idx_frames_employee_time ON activity_frames(employee_id, captured_at);
CREATE INDEX idx_frames_org_time ON activity_frames(organization_id, captured_at);
```

#### Activity Summaries (pre-aggregated per 5-minute window)
```sql
CREATE TABLE activity_summaries (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shift_id        UUID NOT NULL REFERENCES shifts(id) ON DELETE CASCADE,
    employee_id     UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    organization_id UUID NOT NULL REFERENCES organizations(id),
    window_start    TIMESTAMPTZ NOT NULL,
    window_end      TIMESTAMPTZ NOT NULL,
    top_apps        JSONB NOT NULL DEFAULT '[]', -- [{app, seconds, category}]
    idle_seconds    INTEGER NOT NULL DEFAULT 0,
    active_seconds  INTEGER NOT NULL DEFAULT 0,
    window_titles   JSONB NOT NULL DEFAULT '[]',
    productivity_score DECIMAL(4,2), -- 0.0 to 1.0
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_summaries_employee ON activity_summaries(employee_id, window_start);
```

#### Daily AI Summaries
```sql
CREATE TABLE daily_summaries (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id     UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    organization_id UUID NOT NULL REFERENCES organizations(id),
    date            DATE NOT NULL,
    total_active_seconds    INTEGER NOT NULL DEFAULT 0,
    total_idle_seconds      INTEGER NOT NULL DEFAULT 0,
    shift_start     TIMESTAMPTZ,
    shift_end       TIMESTAMPTZ,
    top_apps        JSONB NOT NULL DEFAULT '[]',
    ai_narrative    TEXT,          -- AI-generated daily summary
    ai_model_used   VARCHAR(100),  -- which model generated the summary
    ai_generated_at TIMESTAMPTZ,
    productivity_score DECIMAL(4,2),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (employee_id, date)
);
CREATE INDEX idx_daily_employee ON daily_summaries(employee_id, date);
CREATE INDEX idx_daily_org ON daily_summaries(organization_id, date);
```

#### Audit Log
```sql
CREATE TABLE audit_logs (
    id              BIGSERIAL PRIMARY KEY,
    organization_id UUID REFERENCES organizations(id),
    actor_user_id   UUID REFERENCES users(id),
    action          VARCHAR(100) NOT NULL,  -- e.g., 'VIEW_EMPLOYEE_ACTIVITY', 'DELETE_SESSION'
    resource_type   VARCHAR(50),
    resource_id     VARCHAR(100),
    ip_address      VARCHAR(45),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_audit_org ON audit_logs(organization_id, created_at);
```

---

## Entity Framework Core Models

Create in `apps/SurveilWin.Api/Data/Entities/`:

### Organization.cs
```csharp
public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Plan { get; set; } = "free";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
}
```

### User.cs (with role enum)
```csharp
public enum UserRole { SuperAdmin, OrgAdmin, Manager, Employee }

public class User
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? InvitedBy { get; set; }
    public string? InviteToken { get; set; }
    public DateTime? InviteExpires { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string FullName => $"{FirstName} {LastName}";
}
```

### Shift.cs
```csharp
public enum ShiftStatus { Active, Completed, AutoClosed }

public class Shift
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public User Employee { get; set; } = null!;
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public DateOnly Date { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public decimal ExpectedHours { get; set; } = 8.0m;
    public decimal? ActualHours { get; set; }
    public ShiftStatus Status { get; set; } = ShiftStatus.Active;
    public string? AgentVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<ActivitySession> Sessions { get; set; } = new List<ActivitySession>();
}
```

### ActivityFrame.cs
```csharp
public class ActivityFrame
{
    public long Id { get; set; }
    public Guid SessionId { get; set; }
    public ActivitySession Session { get; set; } = null!;
    public Guid EmployeeId { get; set; }
    public Guid OrganizationId { get; set; }
    public DateTime CapturedAt { get; set; }
    public string ActiveApp { get; set; } = "";
    public string WindowTitle { get; set; } = "";
    public string? AppCategory { get; set; }
    public bool IsIdle { get; set; }
    public string? OcrText { get; set; }
    public short? MonitorIndex { get; set; }
    public int? CursorX { get; set; }
    public int? CursorY { get; set; }
    public string? ThumbnailPath { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

## DbContext

Create `apps/SurveilWin.Api/Data/AppDbContext.cs`:

```csharp
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ManagerAssignment> ManagerAssignments => Set<ManagerAssignment>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<ActivitySession> ActivitySessions => Set<ActivitySession>();
    public DbSet<ActivityFrame> ActivityFrames => Set<ActivityFrame>();
    public DbSet<ActivitySummary> ActivitySummaries => Set<ActivitySummary>();
    public DbSet<DailySummary> DailySummaries => Set<DailySummary>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure table names (snake_case), indexes, composite PKs, JSONB columns
        // Use IEntityTypeConfiguration<T> classes in Data/Configurations/
        // Enable row-level org isolation at query level via global query filters
    }
}
```

---

## API Endpoints

### Auth Controller (`/api/auth`)
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/auth/login` | None | Employee/admin login, returns JWT + refresh token |
| POST | `/api/auth/refresh` | Refresh token | Exchange refresh token for new JWT |
| POST | `/api/auth/logout` | JWT | Invalidate refresh token |
| POST | `/api/auth/accept-invite` | None | Accept invite token, set password |
| POST | `/api/auth/forgot-password` | None | Send password reset email |
| POST | `/api/auth/reset-password` | None | Reset password with token |

### Organizations Controller (`/api/organizations`)
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/organizations` | SuperAdmin | Create new organization |
| GET | `/api/organizations/{id}` | OrgAdmin+ | Get org details |
| PUT | `/api/organizations/{id}` | OrgAdmin | Update org settings |

### Users Controller (`/api/users`)
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/users` | OrgAdmin/Manager | List users in organization |
| POST | `/api/users/invite` | OrgAdmin | Invite new user (sends invite email) |
| GET | `/api/users/{id}` | OrgAdmin/Manager/Self | Get user profile |
| PUT | `/api/users/{id}` | OrgAdmin/Self | Update user |
| DELETE | `/api/users/{id}` | OrgAdmin | Deactivate user |
| POST | `/api/users/{id}/assign-manager` | OrgAdmin | Assign manager to employee |

### Shifts Controller (`/api/shifts`)
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/shifts/start` | Employee | Start a new shift (from agent) |
| POST | `/api/shifts/{id}/end` | Employee | End active shift (from agent) |
| GET | `/api/shifts` | OrgAdmin/Manager | List shifts (filterable by employee, date) |
| GET | `/api/shifts/my` | Employee | Own shifts only |
| GET | `/api/shifts/{id}` | OrgAdmin/Manager/Owner | Shift details |

### Activity Controller (`/api/activity`)
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/activity/frames` | Employee (agent) | Batch upload activity frames |
| POST | `/api/activity/summary` | Employee (agent) | Upload pre-aggregated summary |
| GET | `/api/activity/employee/{id}` | OrgAdmin/Manager | Employee activity timeline |
| GET | `/api/activity/my` | Employee | Own activity data |
| GET | `/api/activity/employee/{id}/screenshots` | OrgAdmin | Screenshots for employee |

### Reports Controller (`/api/reports`)
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/reports/daily/{employeeId}/{date}` | OrgAdmin/Manager | Daily report |
| GET | `/api/reports/weekly/{employeeId}` | OrgAdmin/Manager | Weekly summary |
| GET | `/api/reports/team` | OrgAdmin/Manager | Team overview |
| GET | `/api/reports/export` | OrgAdmin | Export data (CSV/JSON) |

---

## Authentication Implementation

### JWT Configuration (`appsettings.json` additions)
```json
{
  "Jwt": {
    "Secret": "REPLACE_WITH_STRONG_SECRET_32_CHARS_MIN",
    "Issuer": "surveilwin-api",
    "Audience": "surveilwin-clients",
    "ExpiryMinutes": 60,
    "RefreshTokenDays": 30
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=surveilwin;Username=postgres;Password=yourpassword"
  }
}
```

**Never hardcode the JWT secret. Use environment variables in production:**
```bash
export JWT__SECRET="your-production-secret-here"
```

### JWT Token Claims
```csharp
var claims = new[]
{
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new Claim(ClaimTypes.Email, user.Email),
    new Claim(ClaimTypes.Role, user.Role.ToString()),
    new Claim("org_id", user.OrganizationId.ToString()),
    new Claim("full_name", user.FullName)
};
```

### Auth Service (`Services/AuthService.cs`)
Create an `IAuthService` interface and `AuthService` implementation:
- `LoginAsync(string email, string password)` → Returns `AuthResponse { AccessToken, RefreshToken, User }`
- `RefreshTokenAsync(string refreshToken)` → Returns new tokens
- `GenerateJwtToken(User user)` → Creates signed JWT
- `HashPassword(string password)` → BCrypt hash
- `VerifyPassword(string password, string hash)` → BCrypt verify

---

## RBAC Middleware / Policies

Create authorization policies in `Program.cs`:
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OrgAdminOrAbove", policy =>
        policy.RequireRole("OrgAdmin", "SuperAdmin"));
    options.AddPolicy("ManagerOrAbove", policy =>
        policy.RequireRole("Manager", "OrgAdmin", "SuperAdmin"));
    options.AddPolicy("AnyRole", policy =>
        policy.RequireAuthenticatedUser());
});
```

Create a custom `RequireOrgAccessAttribute` that validates the `org_id` claim matches the requested resource's org.

---

## Folder Structure

```
apps/SurveilWin.Api/
├── Controllers/
│   ├── AuthController.cs
│   ├── OrganizationsController.cs
│   ├── UsersController.cs
│   ├── ShiftsController.cs
│   ├── ActivityController.cs
│   └── ReportsController.cs
├── Data/
│   ├── AppDbContext.cs
│   ├── Entities/
│   │   ├── Organization.cs
│   │   ├── User.cs
│   │   ├── ManagerAssignment.cs
│   │   ├── Shift.cs
│   │   ├── ActivitySession.cs
│   │   ├── ActivityFrame.cs
│   │   ├── ActivitySummary.cs
│   │   ├── DailySummary.cs
│   │   └── AuditLog.cs
│   ├── Configurations/
│   │   └── (IEntityTypeConfiguration<T> classes per entity)
│   └── Migrations/
│       └── (auto-generated by EF Core)
├── Services/
│   ├── IAuthService.cs + AuthService.cs
│   ├── IUserService.cs + UserService.cs
│   ├── IShiftService.cs + ShiftService.cs
│   ├── IActivityService.cs + ActivityService.cs
│   ├── IEmailService.cs + EmailService.cs  (for invites)
│   └── IAuditService.cs + AuditService.cs
├── DTOs/
│   ├── Auth/   (LoginRequest, AuthResponse, etc.)
│   ├── Users/  (CreateUserDto, UserDto, InviteUserDto)
│   ├── Shifts/ (StartShiftRequest, ShiftDto)
│   └── Activity/ (FrameBatchRequest, SummaryUploadRequest)
├── Middleware/
│   ├── OrgAccessMiddleware.cs
│   └── AuditMiddleware.cs
├── Extensions/
│   └── ServiceCollectionExtensions.cs
├── appsettings.json
├── appsettings.Development.json
└── Program.cs
```

---

## Program.cs Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts => { /* configure with settings */ });

// Authorization policies
builder.Services.AddAuthorization(/* ... */);

// Application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IShiftService, ShiftService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// CORS (allow web dashboard and agent)
builder.Services.AddCors(opts => opts.AddPolicy("AllowAll", p => p
    .AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(/* add JWT bearer in swagger UI */);

builder.Services.AddControllers();

var app = builder.Build();

// Apply EF migrations on startup (dev only; use CLI for production)
if (app.Environment.IsDevelopment())
    await app.Services.GetRequiredService<AppDbContext>().Database.MigrateAsync();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

---

## Activity Frame Batch Upload API (Critical — used by Agent)

The agent uploads activity frames in batches every 30-60 seconds. Design for efficiency:

### Request DTO (`DTOs/Activity/FrameBatchRequest.cs`)
```csharp
public class FrameBatchRequest
{
    public string SessionKey { get; set; } = "";       // e.g., "session_20260307_090000"
    public string ShiftId { get; set; } = "";          // GUID of active shift
    public List<FrameUploadDto> Frames { get; set; } = new();
}

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
    public string? ThumbnailBase64 { get; set; }  // Optional: base64 WebP thumbnail
}
```

**Performance note:** Accept up to 300 frames per batch (5 minutes of 1 FPS). Use `AddRangeAsync` + single `SaveChangesAsync` for the whole batch. Return `202 Accepted` immediately; persist asynchronously if needed for high load.

---

## Initial Migration & Seeding

After creating all entities and DbContext:
```bash
dotnet ef migrations add InitialCreate --project apps/SurveilWin.Api
dotnet ef database update --project apps/SurveilWin.Api
```

**Seed data (for development):**
- Create default organization: "Demo Corp" / slug: "demo"
- Create SuperAdmin user: admin@surveilwin.com / password: Admin@123
- Create OrgAdmin: owner@demo.com / password: Owner@123
- Create 2 test employees: employee1@demo.com, employee2@demo.com

---

## Docker Support

Create `apps/SurveilWin.Api/Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["apps/SurveilWin.Api/SurveilWin.Api.csproj", "apps/SurveilWin.Api/"]
RUN dotnet restore "apps/SurveilWin.Api/SurveilWin.Api.csproj"
COPY . .
RUN dotnet build "apps/SurveilWin.Api/SurveilWin.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "apps/SurveilWin.Api/SurveilWin.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SurveilWin.Api.dll"]
```

Create `docker-compose.yml` at repository root:
```yaml
version: '3.8'
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: surveilwin
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  api:
    build:
      context: .
      dockerfile: apps/SurveilWin.Api/Dockerfile
    ports:
      - "8080:8080"
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=surveilwin;Username=postgres;Password=postgres
      - Jwt__Secret=REPLACE_WITH_STRONG_SECRET_FOR_DEVELOPMENT_ONLY
      - ASPNETCORE_ENVIRONMENT=Development
    depends_on:
      - postgres

volumes:
  postgres_data:
```

---

## Testing Checklist

After building Phase 1, verify:
- [ ] `POST /api/auth/login` returns JWT for valid credentials
- [ ] `POST /api/auth/login` returns 401 for wrong password
- [ ] `GET /api/users` requires `OrgAdmin` role (returns 403 for Employee)
- [ ] `POST /api/shifts/start` creates a shift for the authenticated employee
- [ ] `POST /api/activity/frames` accepts batch of frames and persists to DB
- [ ] JWT org_id claim prevents cross-org data access
- [ ] Swagger UI shows all endpoints with auth support
- [ ] Docker Compose brings up API + PostgreSQL successfully
- [ ] EF migrations run cleanly

---

## Acceptance Criteria

1. API starts successfully and connects to PostgreSQL
2. All listed endpoints are implemented and return correct HTTP status codes
3. JWT authentication works for all protected endpoints
4. Role-based access control prevents unauthorized access
5. A batch of 100 activity frames can be uploaded and queried
6. Docker Compose environment works end-to-end
7. Swagger documentation is accessible at `/swagger`
