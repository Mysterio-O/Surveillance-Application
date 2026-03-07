import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { getTeamReport } from '../../api/reports';
import { ShiftStatusBadge } from '../../components/shared/ShiftStatusBadge';
import { formatHours } from '../../utils/formatters';

export default function TeamReportPage() {
  const today = new Date().toISOString().split('T')[0];
  const { data, isLoading } = useQuery({
    queryKey: ['team-report', today],
    queryFn: () => getTeamReport(today).then(r => r.data),
  });

  return (
    <div className="space-y-4">
      <h2 className="text-xl font-semibold text-text">Team Report — Today</h2>
      <div className="bg-mantle border border-surface0 rounded-xl overflow-hidden">
        {isLoading ? (
          <div className="p-8 text-center text-subtext0">Loading...</div>
        ) : (
          <table className="w-full">
            <thead>
              <tr className="text-xs text-subtext0 border-b border-surface0">
                <th className="text-left px-4 py-3">Employee</th>
                <th className="text-left px-4 py-3">Status</th>
                <th className="text-left px-4 py-3">Started</th>
                <th className="text-left px-4 py-3">Hours</th>
                <th className="text-left px-4 py-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {(Array.isArray(data) ? data : []).map((s: { employeeId: string; employeeName: string; status: string; startedAt: string; actualHours?: number }) => (
                <tr key={s.employeeId} className="border-b border-surface0/50 hover:bg-surface0/30 transition-colors">
                  <td className="px-4 py-3 text-sm text-text">{s.employeeName}</td>
                  <td className="px-4 py-3"><ShiftStatusBadge status={s.status} /></td>
                  <td className="px-4 py-3 text-sm text-subtext0">
                    {new Date(s.startedAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                  </td>
                  <td className="px-4 py-3 text-sm text-subtext0">{formatHours(s.actualHours)}</td>
                  <td className="px-4 py-3">
                    <Link to={`/activity/${s.employeeId}`} className="text-xs text-blue hover:text-blue/80">View</Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
