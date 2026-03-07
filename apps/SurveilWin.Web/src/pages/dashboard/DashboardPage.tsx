import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { getOrgShifts, getMyShifts } from '../../api/shifts';
import { canViewEmployees } from '../../utils/permissions';
import { ShiftStatusBadge } from '../../components/shared/ShiftStatusBadge';
import { formatHours } from '../../utils/formatters';
import type { UserRole } from '../../types/auth';

export default function DashboardPage() {
  const { user } = useAuth();
  const role = user?.role as UserRole;
  const today = new Date().toISOString().split('T')[0];

  const { data: shifts } = useQuery({
    queryKey: ['shifts', 'today'],
    queryFn: () => canViewEmployees(role)
      ? getOrgShifts(today, today).then(r => r.data)
      : getMyShifts(today, today).then(r => r.data),
    enabled: !!user,
  });

  const active = shifts?.filter(s => s.status === 'Active').length ?? 0;
  const completed = shifts?.filter(s => s.status === 'Completed' || s.status === 'AutoClosed').length ?? 0;

  return (
    <div className="space-y-6">
      <h2 className="text-xl font-semibold text-text">
        {canViewEmployees(role) ? 'Organization Overview' : 'My Dashboard'} — Today
      </h2>

      {canViewEmployees(role) && (
        <div className="grid grid-cols-3 gap-4">
          {[
            { label: 'Active Shifts', value: active, color: 'text-green' },
            { label: 'Completed', value: completed, color: 'text-blue' },
            { label: 'Total Today', value: shifts?.length ?? 0, color: 'text-text' },
          ].map(card => (
            <div key={card.label} className="bg-mantle border border-surface0 rounded-xl p-5">
              <div className={`text-3xl font-bold ${card.color}`}>{card.value}</div>
              <div className="text-subtext0 text-sm mt-1">{card.label}</div>
            </div>
          ))}
        </div>
      )}

      {canViewEmployees(role) && shifts && shifts.length > 0 && (
        <div className="bg-mantle border border-surface0 rounded-xl overflow-hidden">
          <div className="p-4 border-b border-surface0 font-semibold text-text">Employee Activity Today</div>
          <table className="w-full">
            <thead>
              <tr className="text-xs text-subtext0 border-b border-surface0">
                <th className="text-left px-4 py-2">Employee</th>
                <th className="text-left px-4 py-2">Status</th>
                <th className="text-left px-4 py-2">Hours</th>
                <th className="text-left px-4 py-2">Actions</th>
              </tr>
            </thead>
            <tbody>
              {shifts.map(s => (
                <tr key={s.id} className="border-b border-surface0/50 hover:bg-surface0/30 transition-colors">
                  <td className="px-4 py-3 text-sm text-text">{s.employeeName || s.employeeId}</td>
                  <td className="px-4 py-3"><ShiftStatusBadge status={s.status} /></td>
                  <td className="px-4 py-3 text-sm text-subtext0">{formatHours(s.actualHours)}</td>
                  <td className="px-4 py-3">
                    <Link to={`/activity/${s.employeeId}`}
                      className="text-xs text-blue hover:text-blue/80">View</Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {!canViewEmployees(role) && (
        <div className="bg-mantle border border-surface0 rounded-xl p-6">
          <p className="text-subtext0">
            Welcome, <span className="text-text font-semibold">{user?.fullName}</span>!
          </p>
          <div className="mt-4 flex gap-3">
            <Link to="/activity/my" className="bg-blue hover:bg-blue/90 text-base text-sm font-medium px-4 py-2 rounded-lg transition-colors">
              View My Activity
            </Link>
            <Link to="/reports/daily" className="bg-surface0 hover:bg-surface1 text-text text-sm font-medium px-4 py-2 rounded-lg transition-colors">
              Daily Report
            </Link>
          </div>
        </div>
      )}
    </div>
  );
}
