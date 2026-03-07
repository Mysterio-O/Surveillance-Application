# Phase 5 — Web Dashboard with Full RBAC
## SurveilWin Production Platform

---

## Overview

This document instructs an AI agent to build the **React web dashboard** for SurveilWin. The dashboard is the primary interface for administrators and employees to view activity data.

**Deliverable:** A fully functional React 18 + TypeScript web application with role-based views, charts, and activity data display.

---

## Prerequisites

- Phase 1 complete: Backend API is running
- Phase 2 complete: RBAC and user management API exists
- Phase 4 complete: Activity summaries and daily stats are available from API

---

## Tech Stack

| Package | Version | Purpose |
|---------|---------|---------|
| React | 18.x | UI framework |
| TypeScript | 5.x | Type safety |
| Vite | 5.x | Build tool (fast HMR) |
| React Router DOM | 6.x | Client-side routing |
| TanStack Query (React Query) | 5.x | Server state management |
| Axios | 1.x | HTTP client |
| TailwindCSS | 3.x | Utility-first CSS |
| shadcn/ui | latest | Accessible component library |
| Recharts | 2.x | Charts (pie, bar, line) |
| date-fns | 3.x | Date formatting |
| Lucide React | latest | Icons |
| React Hot Toast | 2.x | Notifications |
| Zustand | 4.x | Auth state (lightweight) |

---

## Project Setup

```bash
# Create project inside the repository
npm create vite@latest apps/SurveilWin.Web -- --template react-ts
cd apps/SurveilWin.Web

# Install dependencies
npm install react-router-dom @tanstack/react-query axios date-fns recharts lucide-react react-hot-toast zustand
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init -p

# Install shadcn/ui
npx shadcn-ui@latest init
npx shadcn-ui@latest add button card badge avatar table tabs dialog form input label select separator
```

---

## Folder Structure

```
apps/SurveilWin.Web/
├── src/
│   ├── api/
│   │   ├── client.ts           # Axios instance with JWT interceptor
│   │   ├── auth.ts             # Auth API calls
│   │   ├── users.ts            # User management API
│   │   ├── shifts.ts           # Shift API
│   │   ├── activity.ts         # Activity data API
│   │   └── reports.ts          # Reports/export API
│   ├── components/
│   │   ├── layout/
│   │   │   ├── AppShell.tsx    # Root layout with sidebar + topbar
│   │   │   ├── Sidebar.tsx     # Navigation sidebar (role-aware)
│   │   │   └── TopBar.tsx      # Top navigation bar
│   │   ├── charts/
│   │   │   ├── AppUsagePieChart.tsx
│   │   │   ├── ProductivityTimeline.tsx
│   │   │   └── HourlyActivityBar.tsx
│   │   ├── activity/
│   │   │   ├── ActivityTable.tsx
│   │   │   ├── FrameViewer.tsx
│   │   │   └── ScreenshotGallery.tsx
│   │   └── shared/
│   │       ├── CategoryBadge.tsx   # Colored badge for app categories
│   │       ├── ProductivityScore.tsx
│   │       ├── ShiftStatusBadge.tsx
│   │       └── DateRangePicker.tsx
│   ├── pages/
│   │   ├── auth/
│   │   │   ├── LoginPage.tsx
│   │   │   └── AcceptInvitePage.tsx
│   │   ├── dashboard/
│   │   │   └── DashboardPage.tsx   # Org overview (Admin/Manager)
│   │   ├── employees/
│   │   │   ├── EmployeeListPage.tsx
│   │   │   ├── EmployeeDetailPage.tsx
│   │   │   └── InviteEmployeePage.tsx
│   │   ├── activity/
│   │   │   ├── MyActivityPage.tsx      # Employee self-service
│   │   │   └── EmployeeActivityPage.tsx # Admin view of one employee
│   │   ├── reports/
│   │   │   ├── DailyReportPage.tsx
│   │   │   └── TeamReportPage.tsx
│   │   ├── settings/
│   │   │   ├── OrgSettingsPage.tsx    # Admin only
│   │   │   └── ProfileSettingsPage.tsx
│   │   └── NotFoundPage.tsx
│   ├── hooks/
│   │   ├── useAuth.ts           # Auth state from Zustand
│   │   ├── useEmployee.ts       # Employee data hooks
│   │   └── useActivity.ts       # Activity data hooks
│   ├── stores/
│   │   └── authStore.ts         # Zustand auth store
│   ├── types/
│   │   ├── auth.ts
│   │   ├── user.ts
│   │   ├── shift.ts
│   │   ├── activity.ts
│   │   └── reports.ts
│   ├── utils/
│   │   ├── formatters.ts        # Duration, date, score formatting
│   │   └── permissions.ts       # Role-based permission helpers
│   ├── App.tsx
│   ├── main.tsx
│   └── router.tsx               # Route definitions with guards
├── public/
│   └── favicon.ico
├── index.html
├── tailwind.config.ts
├── tsconfig.json
└── vite.config.ts
```

---

## Authentication & Routing

### Auth Store (`src/stores/authStore.ts`)

```typescript
interface AuthState {
  user: {
    id: string;
    email: string;
    fullName: string;
    role: 'SuperAdmin' | 'OrgAdmin' | 'Manager' | 'Employee';
    orgId: string;
  } | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  login: (token: string, user: AuthState['user']) => void;
  logout: () => void;
}
```

Store tokens in `localStorage` with key `surveilwin_auth`.

### Route Guards (`src/router.tsx`)

```typescript
// Protected route wrapper
function ProtectedRoute({ roles, children }: { roles?: UserRole[], children: ReactNode }) {
  const { user, isAuthenticated } = useAuth();
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  if (roles && !roles.includes(user!.role)) return <Navigate to="/dashboard" replace />;
  return <>{children}</>;
}

// Route definitions
const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  { path: '/accept-invite', element: <AcceptInvitePage /> },
  {
    path: '/',
    element: <ProtectedRoute><AppShell /></ProtectedRoute>,
    children: [
      { index: true, element: <Navigate to="/dashboard" /> },
      { path: 'dashboard', element: <DashboardPage /> },
      {
        path: 'employees',
        element: <ProtectedRoute roles={['OrgAdmin', 'Manager', 'SuperAdmin']}>
          <Outlet />
        </ProtectedRoute>,
        children: [
          { index: true, element: <EmployeeListPage /> },
          { path: ':id', element: <EmployeeDetailPage /> },
          { path: 'invite', element: <InviteEmployeePage /> },
        ]
      },
      { path: 'activity/my', element: <MyActivityPage /> },
      { path: 'activity/:employeeId', element: <EmployeeActivityPage /> },
      { path: 'reports/daily', element: <DailyReportPage /> },
      { path: 'reports/team', element: <TeamReportPage /> },
      { path: 'settings/org', element: <OrgSettingsPage /> },
      { path: 'settings/profile', element: <ProfileSettingsPage /> },
    ]
  }
]);
```

---

## API Client (`src/api/client.ts`)

```typescript
const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:8080',
  headers: { 'Content-Type': 'application/json' }
});

// Attach JWT to every request
api.interceptors.request.use((config) => {
  const auth = JSON.parse(localStorage.getItem('surveilwin_auth') || '{}');
  if (auth.accessToken) {
    config.headers.Authorization = `Bearer ${auth.accessToken}`;
  }
  return config;
});

// Handle 401 — auto logout
api.interceptors.response.use(
  (res) => res,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('surveilwin_auth');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);
```

---

## Pages Implementation

### Login Page (`src/pages/auth/LoginPage.tsx`)

- Clean centered card with SurveilWin brand
- Email + password inputs
- Submit calls `POST /api/auth/login`
- On success: store token in Zustand + localStorage, redirect to `/dashboard`
- Dark theme matching agent UI (#1e1e2e background, #89b4fa accents)
- Show loading spinner on submit

---

### Dashboard Page (`src/pages/dashboard/DashboardPage.tsx`)

**Admin/Manager view:**
```
┌─────────────────────────────────────────────────────────┐
│  Organization Overview — Today                          │
├────────────┬───────────────┬──────────────┬─────────────┤
│  Active    │  Completed    │  Not Started │  Total      │
│  Shifts: 5 │  Shifts: 3   │  Shifts: 2  │  Employees  │
│            │               │              │  10         │
└────────────┴───────────────┴──────────────┴─────────────┘

Employee Activity Today
┌────────────────────────────────────────────────────────────┐
│ Name          │ Status    │ Hours │ Productivity │ Actions │
├────────────────────────────────────────────────────────────┤
│ John Doe      │ 🟢 Active  │ 5.2h  │ ████████ 82% │ View   │
│ Jane Smith    │ 🟢 Active  │ 4.1h  │ ██████   61% │ View   │
│ Bob Wilson    │ ⚪ Offline │ 0h    │ —            │ View   │
└────────────────────────────────────────────────────────────┘
```

**Employee self-service view:**
Shows only their own current shift status + quick link to their activity.

---

### Employee Detail Page (`src/pages/employees/EmployeeDetailPage.tsx`)

**Admin view of one employee:**

```
┌──────────────────────────────────────────────────────────────┐
│  ← Back    John Doe  |  Employee  |  john@company.com        │
├──────────────────────────────────────────────────────────────┤
│  [Today] [This Week] [This Month] [Custom Range]             │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  SUMMARY CARDS                                               │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐   │
│  │ Active   │ │ Hours    │ │ Prod.    │ │ Top App      │   │
│  │ Today    │ │ 6.5h     │ │ Score    │ │ VS Code      │   │
│  │ 🟢 Yes   │ │ (of 8h)  │ │ 78%      │ │ 4.2 hrs      │   │
│  └──────────┘ └──────────┘ └──────────┘ └──────────────┘   │
│                                                              │
│  APP USAGE (Pie Chart)          HOURLY PRODUCTIVITY (Bar)   │
│  ┌──────────────────────┐       ┌──────────────────────┐    │
│  │    [Recharts Pie]    │       │   [Recharts Bar]     │    │
│  │  Coding 52%          │       │  9am ████████ 85%   │    │
│  │  Browser 23%         │       │ 10am ███████  72%   │    │
│  │  Slack 12%           │       │ 11am ██████   62%   │    │
│  └──────────────────────┘       └──────────────────────┘    │
│                                                              │
│  ACTIVITY TIMELINE (scrollable list of 5-min windows)       │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ 09:00 – 09:05  │ VS Code  │ coding  │ ████████ 90% │   │
│  │ 09:05 – 09:10  │ Chrome   │ browser │ ██████   70% │   │
│  │ 09:10 – 09:15  │ Slack    │ comms   │ █████    60% │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
│  AI DAILY SUMMARY (shown if AI enabled)                     │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ John worked for 6.5 hours today, primarily on        │   │
│  │ development work in VS Code (52% of time). He had    │   │
│  │ several code review sessions on GitHub and attended  │   │
│  │ one team meeting on Slack...                         │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
│  SCREENSHOTS GALLERY (admin only, if screenshots enabled)   │
│  ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐               │
│  │ 🖼 │ │ 🖼 │ │ 🖼 │ │ 🖼 │ │ 🖼 │ │ 🖼 │ ...           │
│  └────┘ └────┘ └────┘ └────┘ └────┘ └────┘               │
└──────────────────────────────────────────────────────────────┘
```

---

### My Activity Page (`src/pages/activity/MyActivityPage.tsx`)

**Employee self-service — sees only their own data:**

Same layout as Employee Detail Page, but:
- No screenshots section (employees don't see their own screenshots by default)
- Title: "My Activity"
- Shows their own AI daily summary
- Date selector limited to their own shifts

---

### Employee List Page (`src/pages/employees/EmployeeListPage.tsx`)

```
┌──────────────────────────────────────────────────────────────┐
│  Team Members                          [+ Invite Employee]   │
│  Search: [________________] Role: [All ▾] Status: [All ▾]   │
├──────────────────────────────────────────────────────────────┤
│  Name            │ Role     │ Status   │ Last Active │ Actn  │
├──────────────────────────────────────────────────────────────┤
│  John Doe        │ Employee │ 🟢 Active │ 2 min ago  │ ···   │
│  Jane Smith      │ Employee │ 🟢 Active │ 5 min ago  │ ···   │
│  Mike Johnson    │ Manager  │ ⚪ Offline │ Yesterday │ ···   │
└──────────────────────────────────────────────────────────────┘
```

Row action menu (···): View Activity, Edit User, Assign Manager, Deactivate

---

### Invite Employee Page

Form with:
- Email (required)
- First Name, Last Name
- Role selector (Employee, Manager — OrgAdmin cannot invite OrgAdmin through this UI)
- Assign to Manager (dropdown, optional)
- Submit → `POST /api/users/invite`
- Show success: "Invite sent to john@company.com"

---

### Org Settings Page (`src/pages/settings/OrgSettingsPage.tsx`)

**Tabs:**
1. **General** — org name, logo upload
2. **Monitoring Policy** — capture FPS, OCR toggle, screenshots toggle, interval, allowed/denied apps
3. **Shift Policy** — expected hours, auto-close hours
4. **AI Summaries** — enable/disable, AI provider selector (Ollama URL, OpenAI key, Gemini key)
5. **Retention** — screenshot retention days, summary retention days

---

## Sidebar Navigation (Role-Aware)

```typescript
const navItems = [
  { icon: LayoutDashboard, label: 'Dashboard', path: '/dashboard', roles: ['all'] },
  { icon: Users, label: 'Employees', path: '/employees', roles: ['OrgAdmin', 'Manager', 'SuperAdmin'] },
  { icon: Activity, label: 'My Activity', path: '/activity/my', roles: ['all'] },
  { icon: BarChart2, label: 'Team Report', path: '/reports/team', roles: ['OrgAdmin', 'Manager', 'SuperAdmin'] },
  { icon: FileText, label: 'Daily Report', path: '/reports/daily', roles: ['all'] },
  { icon: Settings, label: 'Org Settings', path: '/settings/org', roles: ['OrgAdmin', 'SuperAdmin'] },
  { icon: UserCircle, label: 'Profile', path: '/settings/profile', roles: ['all'] },
];
```

---

## Charts Implementation

### App Usage Pie Chart (`src/components/charts/AppUsagePieChart.tsx`)

```typescript
// Uses Recharts PieChart with COLORS mapped to categories:
const CATEGORY_COLORS = {
  coding:         '#a6e3a1', // green
  browser_work:   '#89b4fa', // blue
  browser:        '#74c7ec', // sky blue
  communication:  '#cba6f7', // mauve/purple
  docs:           '#f9e2af', // yellow
  terminal:       '#94e2d5', // teal
  media:          '#f38ba8', // red/pink (non-productive)
  system:         '#a6adc8', // subtext
  idle:           '#313244', // dim
  other:          '#585b70', // surface
};
```

### Productivity Timeline (`src/components/charts/ProductivityTimeline.tsx`)

Line chart showing productivity score (0–100%) over hours of the day.
Color gradient: 0–40% red, 41–70% yellow, 71–100% green.

### Hourly Activity Bar (`src/components/charts/HourlyActivityBar.tsx`)

Stacked bar chart per hour showing category breakdown.

---

## Key TypeScript Types

### `src/types/activity.ts`
```typescript
export interface ActivitySummaryWindow {
  windowStart: string;
  windowEnd: string;
  topApps: AppDwellTime[];
  idleSeconds: number;
  activeSeconds: number;
  productivityScore: number;
}

export interface AppDwellTime {
  app: string;
  displayName: string;
  category: AppCategory;
  seconds: number;
  percent: number;
}

export type AppCategory =
  'coding' | 'browser_work' | 'browser' | 'communication' |
  'docs' | 'terminal' | 'media' | 'system' | 'idle' | 'other';

export interface DailyReport {
  date: string;
  employee: { id: string; fullName: string; };
  shift: { startedAt: string; endedAt: string; actualHours: number; } | null;
  totals: { activeSeconds: number; idleSeconds: number; productivityScore: number; };
  appBreakdown: AppDwellTime[];
  hourlyProductivity: { hour: number; score: number; dominant: AppCategory; }[];
  jiraTickets: string[];
  topKeywords: string[];
  aiNarrative?: string;
}
```

---

## Category Badge Component

```tsx
// src/components/shared/CategoryBadge.tsx
const CATEGORY_LABELS: Record<AppCategory, string> = {
  coding:         '💻 Coding',
  browser_work:   '🌐 Work Browser',
  browser:        '🌐 Browser',
  communication:  '💬 Communication',
  docs:           '📄 Documents',
  terminal:       '⬛ Terminal',
  media:          '🎵 Media',
  system:         '⚙️ System',
  idle:           '💤 Idle',
  other:          '🔷 Other',
};

export function CategoryBadge({ category }: { category: AppCategory }) {
  return (
    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${CATEGORY_STYLES[category]}`}>
      {CATEGORY_LABELS[category]}
    </span>
  );
}
```

---

## Export Reports

### `GET /api/reports/export?employeeId=X&from=DATE&to=DATE&format=csv`

Add an export button on the Employee Detail and Team Report pages:
```typescript
async function exportReport(employeeId: string, from: string, to: string, format: 'csv' | 'json') {
  const url = `/api/reports/export?employeeId=${employeeId}&from=${from}&to=${to}&format=${format}`;
  const response = await api.get(url, { responseType: 'blob' });
  // Trigger browser download
  const link = document.createElement('a');
  link.href = URL.createObjectURL(response.data);
  link.download = `activity_${employeeId}_${from}_${to}.${format}`;
  link.click();
}
```

---

## Environment Variables

Create `apps/SurveilWin.Web/.env.development`:
```
VITE_API_URL=http://localhost:8080
VITE_APP_NAME=SurveilWin
```

Create `apps/SurveilWin.Web/.env.production`:
```
VITE_API_URL=https://api.yourdomain.com
VITE_APP_NAME=SurveilWin
```

---

## Build & Serve

```bash
# Development
cd apps/SurveilWin.Web
npm run dev   # starts on http://localhost:5173

# Production build
npm run build  # outputs to dist/

# Serve production build locally
npm run preview
```

**Add to Docker Compose** (`docker-compose.yml`):
```yaml
  web:
    build:
      context: apps/SurveilWin.Web
      dockerfile: Dockerfile
    ports:
      - "3000:80"
    environment:
      - VITE_API_URL=http://api:8080
```

Create `apps/SurveilWin.Web/Dockerfile`:
```dockerfile
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
```

---

## Testing Checklist

- [ ] Login page authenticates and redirects correctly
- [ ] Admin sees "Employees" link in sidebar; Employee does not
- [ ] Admin can view any employee's activity
- [ ] Employee can only see their own activity (accessing other IDs returns 403)
- [ ] App usage pie chart renders with correct category colors
- [ ] Productivity score bar updates in real-time during shift
- [ ] Date range picker filters activity data correctly
- [ ] Export button downloads CSV with activity data
- [ ] Invite flow works: invite sent → link accepted → user appears in list
- [ ] Org settings page saves monitoring policy changes
- [ ] Dark theme is consistent throughout

---

## Acceptance Criteria

1. Admin can log in and see all employees with their live shift status
2. Admin can click any employee and see their full activity breakdown with charts
3. Employee can log in and see only their own activity
4. App usage chart shows category-colored breakdown
5. AI daily summary is displayed (if AI is configured)
6. Screenshot gallery shows thumbnails for admin (if screenshots enabled)
7. Export CSV works and downloads activity data
8. Dashboard is responsive and works in modern browsers (Chrome, Firefox, Edge)
