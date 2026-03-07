# Phase 2 — Multi-Role RBAC System, Organization Management & Invite Flow
## SurveilWin Production Platform

---

## Overview

This document instructs an AI agent to implement the **complete multi-role system** for SurveilWin. This phase builds on Phase 1 (the backend API and database).

**Deliverable:** Fully functional role-based access control, organization management, user invite flow, and shift scheduling policies.

---

## Prerequisites

- Phase 1 complete: ASP.NET Core 8 API is running with PostgreSQL
- Entities exist: `Organization`, `User`, `Shift`, `ManagerAssignment`
- JWT authentication is working

---

## Role Definitions

### Role Hierarchy

```
SuperAdmin
    └── OrgAdmin (one or more per organization)
            └── Manager (optional middle tier)
                    └── Employee (leaf — does the actual work)
```

### Role Capabilities Matrix

| Capability | SuperAdmin | OrgAdmin | Manager | Employee |
|-----------|-----------|---------|---------|---------|
| Create organizations | ✅ | ❌ | ❌ | ❌ |
| View all organizations | ✅ | ❌ | ❌ | ❌ |
| Manage org settings | ✅ | ✅ | ❌ | ❌ |
| Invite OrgAdmin | ✅ | ❌ | ❌ | ❌ |
| Invite Manager | ✅ | ✅ | ❌ | ❌ |
| Invite Employee | ✅ | ✅ | ✅ | ❌ |
| View ALL employee activity | ✅ | ✅ | ❌ | ❌ |
| View ASSIGNED employee activity | ✅ | ✅ | ✅ | ❌ |
| View own activity | ✅ | ✅ | ✅ | ✅ |
| View screenshots (all) | ✅ | ✅ | ❌ | ❌ |
| View own screenshots | ✅ | ✅ | ✅ | ✅ |
| Download/export reports | ✅ | ✅ | ✅ (team) | ✅ (self) |
| Delete employee data | ✅ | ✅ | ❌ | ❌ |
| Assign managers to employees | ✅ | ✅ | ❌ | ❌ |
| Start/end own shift | ✅ | ✅ | ✅ | ✅ |
| Force-close employee shift | ✅ | ✅ | ❌ | ❌ |
| Configure org monitoring policy | ✅ | ✅ | ❌ | ❌ |

---

## Implementation: Authorization Service

Create `apps/SurveilWin.Api/Services/AuthorizationService.cs`:

```csharp
public interface IResourceAuthorizationService
{
    /// Can the requesting user view activity data for the target employee?
    Task<bool> CanViewEmployeeActivityAsync(Guid requestorId, Guid targetEmployeeId);

    /// Can the requesting user manage (edit/delete) the target user?
    Task<bool> CanManageUserAsync(Guid requestorId, Guid targetUserId);

    /// Is the requesting user in the same organization as the target?
    Task<bool> InSameOrgAsync(Guid requestorId, Guid targetId);
}
```

**Implementation rules:**
- `SuperAdmin` can access anything
- `OrgAdmin` can access anyone in their org
- `Manager` can only access employees in their `manager_assignments` table
- `Employee` can only access their own data (must match `requestorId == targetEmployeeId`)

---

## Invite Flow

### How Invites Work

1. OrgAdmin calls `POST /api/users/invite` with `{email, role, firstName, lastName}`
2. System creates a `User` record with `InviteToken` (random 32-byte GUID) and `InviteExpires` (48 hours)
3. System sends invite email with link: `https://dashboard.surveilwin.com/accept-invite?token=<TOKEN>`
4. New user visits link, sets their password via `POST /api/auth/accept-invite`
5. `InviteToken` and `InviteExpires` are cleared; user is now active

### Invite Email Template (plain text fallback)

Subject: `You've been invited to SurveilWin — {OrgName}`

Body:
```
Hi {FirstName},

{InviterName} has invited you to join {OrgName} on SurveilWin as {Role}.

Click the link below to set up your account (valid for 48 hours):
{InviteUrl}

If you did not expect this invitation, you can safely ignore this email.

— The SurveilWin Team
```

### Email Service

Create `IEmailService` interface with implementations:
1. **SmtpEmailService** — uses `System.Net.Mail.SmtpClient` with configurable SMTP server
2. **LoggingEmailService** (development) — just logs invite URL to console instead of sending

Configure in `appsettings.json`:
```json
{
  "Email": {
    "Provider": "Smtp",   // "Smtp" or "Log" (development)
    "Smtp": {
      "Host": "smtp.gmail.com",
      "Port": 587,
      "Username": "noreply@yourcompany.com",
      "Password": "REPLACE",
      "FromAddress": "noreply@yourcompany.com",
      "FromName": "SurveilWin"
    }
  },
  "App": {
    "DashboardUrl": "https://dashboard.surveilwin.com"
  }
}
```

---

## Organization Policy Settings

Add `OrgPolicy` entity/table for per-organization monitoring configuration:

```sql
CREATE TABLE org_policies (
    organization_id     UUID PRIMARY KEY REFERENCES organizations(id) ON DELETE CASCADE,
    capture_fps         DECIMAL(4,1) NOT NULL DEFAULT 1.0,
    enable_ocr          BOOLEAN NOT NULL DEFAULT TRUE,
    enable_screenshots  BOOLEAN NOT NULL DEFAULT FALSE,
    screenshot_interval_minutes INTEGER NOT NULL DEFAULT 5,
    screenshot_retention_days   INTEGER NOT NULL DEFAULT 7,
    summary_retention_days      INTEGER NOT NULL DEFAULT 90,
    allowed_apps        JSONB NOT NULL DEFAULT '[]',   -- whitelist
    denied_apps         JSONB NOT NULL DEFAULT '[]',   -- blacklist
    expected_shift_hours DECIMAL(4,2) NOT NULL DEFAULT 8.0,
    auto_close_shift_after_hours INTEGER NOT NULL DEFAULT 12,  -- auto-end shift after N hours
    enable_ai_summaries BOOLEAN NOT NULL DEFAULT TRUE,
    ai_provider         VARCHAR(50) DEFAULT 'ollama',  -- ollama, openai, gemini
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Policy API Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/organizations/{id}/policy` | OrgAdmin | Get org monitoring policy |
| PUT | `/api/organizations/{id}/policy` | OrgAdmin | Update policy |
| GET | `/api/organizations/{id}/policy/agent-config` | Employee (agent) | Fetch agent config on login |

**The agent config endpoint** returns a minimal JSON config for the agent to apply:
```json
{
  "captureFps": 1.0,
  "enableOcr": true,
  "enableScreenshots": false,
  "screenshotIntervalMinutes": 5,
  "allowedApps": [],
  "deniedApps": ["steam.exe", "epicgameslauncher.exe"],
  "expectedShiftHours": 8.0
}
```

The agent fetches this on startup and applies it, overriding local `appsettings.json`. This allows admins to enforce monitoring settings remotely.

---

## Shift Management

### Shift Service (`Services/ShiftService.cs`)

```csharp
public interface IShiftService
{
    Task<Shift> StartShiftAsync(Guid employeeId, string agentVersion);
    Task<Shift> EndShiftAsync(Guid shiftId, Guid employeeId);
    Task AutoCloseStaleShiftsAsync(); // Background job
    Task<Shift?> GetActiveShiftAsync(Guid employeeId);
    Task<IEnumerable<Shift>> GetEmployeeShiftsAsync(Guid employeeId, DateOnly from, DateOnly to);
    Task<IEnumerable<Shift>> GetOrgShiftsAsync(Guid orgId, DateOnly from, DateOnly to);
}
```

**Start Shift rules:**
- Check if employee already has an active shift → if yes, return the existing shift (idempotent)
- Only one active shift per employee at a time
- Record `agentVersion` for compatibility tracking

**End Shift rules:**
- Calculate `actual_hours` = `(ended_at - started_at).TotalHours`
- Set `status = Completed`

**Auto-close background job:**
- Run every hour via hosted service or Hangfire
- Find all shifts with `status = Active` AND `started_at < NOW() - auto_close_after_hours`
- Set `status = AutoClosed`, `ended_at = NOW()`
- Log audit event

---

## User Management Endpoints (Full Implementation)

### `POST /api/users/invite`

**Request:**
```json
{
  "email": "john@company.com",
  "role": "Employee",
  "firstName": "John",
  "lastName": "Doe",
  "managerId": "optional-uuid-if-assigning-manager-immediately"
}
```

**Logic:**
1. Validate caller is OrgAdmin or higher
2. Validate role they're inviting is not higher than their own role
3. Check email not already registered in this org
4. Create User with `InviteToken = GenerateSecureToken()`, `IsActive = false`
5. If `managerId` provided, create `ManagerAssignment`
6. Send invite email
7. Return `201 Created` with user ID

### `POST /api/auth/accept-invite`

**Request:**
```json
{
  "token": "invite-token-from-email",
  "password": "NewPassword@123",
  "confirmPassword": "NewPassword@123"
}
```

**Logic:**
1. Find user by `InviteToken` where `InviteExpires > NOW()`
2. Validate passwords match and meet complexity requirements
3. Hash password, set `IsActive = true`, clear `InviteToken`/`InviteExpires`
4. Return JWT tokens (log user in immediately)

### Password Requirements
- Minimum 8 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- At least one special character

---

## Manager Assignment API

### `POST /api/users/assignments`
```json
{
  "managerId": "uuid",
  "employeeIds": ["uuid1", "uuid2"]
}
```

**Rules:**
- Manager must exist and have role `Manager` or `OrgAdmin`
- All employees must be in same organization
- Only OrgAdmin can make assignments

### `GET /api/users/my-team` (Manager view)
Returns list of employees assigned to the authenticated manager.

---

## Role Upgrade / Downgrade

### `PUT /api/users/{id}/role`
```json
{ "role": "Manager" }
```

**Rules:**
- Only OrgAdmin can change roles within their org
- Cannot set role higher than caller's own role
- Cannot demote the last OrgAdmin in an organization
- Log audit event on every role change

---

## Audit Logging

Every sensitive action must be logged. Create an `AuditMiddleware` or call `IAuditService` explicitly:

**Events to log:**
| Action | Triggered By |
|--------|-------------|
| `USER_INVITED` | OrgAdmin invites new user |
| `USER_ROLE_CHANGED` | Admin changes user role |
| `USER_DEACTIVATED` | Admin deactivates account |
| `SHIFT_FORCE_CLOSED` | Admin closes employee shift |
| `ACTIVITY_VIEWED` | Admin views employee activity data |
| `SCREENSHOT_VIEWED` | Admin views employee screenshot |
| `DATA_EXPORTED` | Admin downloads data export |
| `DATA_DELETED` | Admin deletes employee data |
| `POLICY_UPDATED` | Admin changes org monitoring policy |

---

## Shift Scheduling (Optional Enhancement)

Add a `ShiftSchedule` table for planned schedules:

```sql
CREATE TABLE shift_schedules (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id     UUID NOT NULL REFERENCES users(id),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    day_of_week     SMALLINT NOT NULL,  -- 0=Monday, 6=Sunday
    expected_start  TIME NOT NULL,      -- e.g., 09:00
    expected_end    TIME NOT NULL,      -- e.g., 17:00
    timezone        VARCHAR(100) NOT NULL DEFAULT 'UTC',
    is_active       BOOLEAN NOT NULL DEFAULT TRUE
);
```

This allows the system to flag when an employee hasn't started their expected shift.

---

## Testing Checklist

- [ ] OrgAdmin can invite Employee, Manager roles
- [ ] Manager can invite Employee role only
- [ ] Employee cannot invite anyone (403)
- [ ] Invite token is single-use and expires after 48 hours
- [ ] Accept invite sets password and activates user
- [ ] Manager can only view assigned employees' data
- [ ] OrgAdmin can view all employees in their org
- [ ] Cross-org data access is prevented (employee from Org A cannot access Org B data)
- [ ] Role changes are audit-logged
- [ ] Auto-close background job closes stale shifts
- [ ] `/api/organizations/{id}/policy/agent-config` returns correct config per org

---

## Acceptance Criteria

1. Invite-to-onboard flow works end-to-end (email → accept → login)
2. Manager can only see their assigned employees' data
3. OrgAdmin can see all org employees
4. Org policy settings are applied to agent on login
5. Auto-close shift job runs and closes stale shifts
6. All sensitive actions appear in the audit log
