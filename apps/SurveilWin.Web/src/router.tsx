import { createBrowserRouter, Navigate, Outlet } from 'react-router-dom';
import { useAuthStore } from './stores/authStore';
import { AppShell } from './components/layout/AppShell';
import LoginPage from './pages/auth/LoginPage';
import AcceptInvitePage from './pages/auth/AcceptInvitePage';
import DashboardPage from './pages/dashboard/DashboardPage';
import EmployeeListPage from './pages/employees/EmployeeListPage';
import EmployeeActivityPage from './pages/activity/EmployeeActivityPage';
import InviteEmployeePage from './pages/employees/InviteEmployeePage';
import MyActivityPage from './pages/activity/MyActivityPage';
import DailyReportPage from './pages/reports/DailyReportPage';
import TeamReportPage from './pages/reports/TeamReportPage';
import OrgSettingsPage from './pages/settings/OrgSettingsPage';
import ProfileSettingsPage from './pages/settings/ProfileSettingsPage';
import NotFoundPage from './pages/NotFoundPage';
import type { UserRole } from './types/auth';

function ProtectedRoute({ roles, children }: { roles?: UserRole[]; children: React.ReactNode }) {
  const { isAuthenticated, user } = useAuthStore();
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  if (roles && user && !roles.includes(user.role as UserRole)) return <Navigate to="/dashboard" replace />;
  return <>{children}</>;
}

export const router = createBrowserRouter([
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
        element: <ProtectedRoute roles={['OrgAdmin', 'Manager', 'SuperAdmin']}><Outlet /></ProtectedRoute>,
        children: [
          { index: true, element: <EmployeeListPage /> },
          { path: 'invite', element: <InviteEmployeePage /> },
        ]
      },
      { path: 'activity/my', element: <MyActivityPage /> },
      { path: 'activity/:employeeId', element: <EmployeeActivityPage /> },
      { path: 'reports/daily', element: <DailyReportPage /> },
      { path: 'reports/team', element: <ProtectedRoute roles={['OrgAdmin', 'Manager', 'SuperAdmin']}><TeamReportPage /></ProtectedRoute> },
      { path: 'settings/org', element: <ProtectedRoute roles={['OrgAdmin', 'SuperAdmin']}><OrgSettingsPage /></ProtectedRoute> },
      { path: 'settings/profile', element: <ProfileSettingsPage /> },
    ]
  },
  { path: '*', element: <NotFoundPage /> },
]);
