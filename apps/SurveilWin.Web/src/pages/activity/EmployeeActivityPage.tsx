import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { getUser } from '../../api/users';
import { getDailyReport } from '../../api/reports';
import { getEmployeeActivity } from '../../api/activity';
import { AppUsagePieChart } from '../../components/charts/AppUsagePieChart';
import { ProductivityTimeline } from '../../components/charts/ProductivityTimeline';
import { ProductivityScore } from '../../components/shared/ProductivityScore';
import { CategoryBadge } from '../../components/shared/CategoryBadge';
import { formatDuration } from '../../utils/formatters';
import type { AppCategory } from '../../types/activity';

export default function EmployeeActivityPage() {
  const { employeeId } = useParams<{ employeeId: string }>();
  const today = new Date().toISOString().split('T')[0];
  const [date, setDate] = useState(today);

  const { data: employee } = useQuery({
    queryKey: ['user', employeeId],
    queryFn: () => getUser(employeeId!).then(r => r.data),
    enabled: !!employeeId,
  });

  const { data: report } = useQuery({
    queryKey: ['daily-report', employeeId, date],
    queryFn: () => getDailyReport(employeeId!, date).then(r => r.data),
    enabled: !!employeeId,
  });

  const { data: activity } = useQuery({
    queryKey: ['activity', employeeId, date],
    queryFn: () => {
      const from = new Date(date); from.setHours(0, 0, 0, 0);
      const to = new Date(date); to.setHours(23, 59, 59, 999);
      return getEmployeeActivity(employeeId!, from.toISOString(), to.toISOString()).then(r => r.data);
    },
    enabled: !!employeeId,
  });

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Link to="/employees" className="text-subtext0 hover:text-text text-sm">← Back</Link>
        <div>
          <h2 className="text-xl font-semibold text-text">{employee?.fullName ?? 'Employee'}</h2>
          <p className="text-subtext0 text-sm">{employee?.email}</p>
        </div>
        <div className="ml-auto">
          <input type="date" value={date} max={today} onChange={e => setDate(e.target.value)}
            className="bg-mantle border border-surface0 text-text rounded-lg px-3 py-2 text-sm focus:outline-none focus:border-blue" />
        </div>
      </div>

      {report && (
        <div className="grid grid-cols-4 gap-4">
          {[
            { label: 'Active Time', value: formatDuration(report.totals.activeSeconds) },
            { label: 'Idle Time', value: formatDuration(report.totals.idleSeconds) },
            { label: 'Shift Hours', value: report.shift?.actualHours ? `${report.shift.actualHours.toFixed(1)}h` : '—' },
          ].map(c => (
            <div key={c.label} className="bg-mantle border border-surface0 rounded-xl p-4">
              <div className="text-2xl font-bold text-text">{c.value}</div>
              <div className="text-xs text-subtext0 mt-1">{c.label}</div>
            </div>
          ))}
          <div className="bg-mantle border border-surface0 rounded-xl p-4">
            <div className="text-xs text-subtext0 mb-2">Productivity</div>
            <ProductivityScore score={report.totals.productivityScore} />
          </div>
        </div>
      )}

      <div className="grid grid-cols-2 gap-6">
        <div className="bg-mantle border border-surface0 rounded-xl p-5">
          <h3 className="text-sm font-semibold text-text mb-4">App Usage Breakdown</h3>
          <AppUsagePieChart data={report?.appBreakdown ?? []} />
        </div>
        <div className="bg-mantle border border-surface0 rounded-xl p-5">
          <h3 className="text-sm font-semibold text-text mb-4">Hourly Productivity</h3>
          <ProductivityTimeline data={report?.hourlyProductivity ?? []} />
        </div>
      </div>

      {report?.aiNarrative && (
        <div className="bg-mantle border border-surface0 rounded-xl p-5">
          <h3 className="text-sm font-semibold text-mauve mb-3">✨ AI Daily Summary</h3>
          <p className="text-text text-sm leading-relaxed">{report.aiNarrative}</p>
        </div>
      )}

      {activity && activity.length > 0 && (
        <div className="bg-mantle border border-surface0 rounded-xl overflow-hidden">
          <div className="p-4 border-b border-surface0 text-sm font-semibold text-text">Activity Timeline</div>
          <div className="divide-y divide-surface0/50">
            {activity.slice(0, 50).map(w => (
              <div key={w.id} className="flex items-center gap-4 px-4 py-2.5 hover:bg-surface0/20">
                <span className="text-xs text-subtext0 w-28 shrink-0">
                  {new Date(w.windowStart).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} –
                  {new Date(w.windowEnd).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                </span>
                {w.topApps[0] && (
                  <>
                    <span className="text-sm text-text w-28 truncate">{w.topApps[0].displayName}</span>
                    <CategoryBadge category={w.topApps[0].category as AppCategory} />
                  </>
                )}
                <div className="ml-auto"><ProductivityScore score={w.productivityScore} size="sm" /></div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
