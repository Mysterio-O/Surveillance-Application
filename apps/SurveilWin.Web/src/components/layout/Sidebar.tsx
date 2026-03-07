import { NavLink } from 'react-router-dom';
import { LayoutDashboard, Users, Activity, BarChart2, FileText, Settings, UserCircle, LogOut } from 'lucide-react';
import { useAuth } from '../../hooks/useAuth';
import { canViewEmployees, canViewOrgSettings } from '../../utils/permissions';
import type { UserRole } from '../../types/auth';

export function Sidebar() {
  const { user, logout } = useAuth();
  if (!user) return null;

  const role = user.role as UserRole;

  const navItems = [
    { icon: LayoutDashboard, label: 'Dashboard', path: '/dashboard', show: true },
    { icon: Users,           label: 'Employees',  path: '/employees', show: canViewEmployees(role) },
    { icon: Activity,        label: 'My Activity', path: '/activity/my', show: true },
    { icon: BarChart2,       label: 'Team Report', path: '/reports/team', show: canViewEmployees(role) },
    { icon: FileText,        label: 'Daily Report', path: '/reports/daily', show: true },
    { icon: Settings,        label: 'Org Settings', path: '/settings/org', show: canViewOrgSettings(role) },
    { icon: UserCircle,      label: 'Profile', path: '/settings/profile', show: true },
  ];

  return (
    <div className="w-56 bg-mantle h-full flex flex-col border-r border-surface0">
      <div className="p-4 border-b border-surface0">
        <div className="text-blue font-bold text-lg">SurveilWin</div>
        <div className="text-subtext0 text-xs mt-0.5">{user.orgName || 'Organization'}</div>
      </div>

      <nav className="flex-1 p-3 space-y-1">
        {navItems.filter(i => i.show).map(item => (
          <NavLink key={item.path} to={item.path}
            className={({ isActive }) =>
              `flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors ${
                isActive ? 'bg-surface0 text-text' : 'text-subtext0 hover:bg-surface0/50 hover:text-text'
              }`}>
            <item.icon size={16} />
            {item.label}
          </NavLink>
        ))}
      </nav>

      <div className="p-3 border-t border-surface0">
        <div className="text-xs text-subtext0 mb-2 px-3">{user.fullName}</div>
        <div className="text-xs text-subtext0/60 px-3 mb-2">{user.role}</div>
        <button onClick={logout}
          className="flex items-center gap-3 px-3 py-2 w-full rounded-lg text-sm text-subtext0 hover:bg-surface0/50 hover:text-red transition-colors">
          <LogOut size={16} />
          Sign Out
        </button>
      </div>
    </div>
  );
}
