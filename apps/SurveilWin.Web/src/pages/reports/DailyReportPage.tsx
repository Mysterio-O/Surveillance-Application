import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useAuth } from '../../hooks/useAuth';
import { getDailyReport } from '../../api/reports';
import { AppUsagePieChart } from '../../components/charts/AppUsagePieChart';
import { ProductivityScore } from '../../components/shared/ProductivityScore';
import { formatDuration } from '../../utils/formatters';

export default function DailyReportPage() {
  const { user } = useAuth();
  const today = new Date().toISOString().split('T')[0];
  const [date, setDate] = useState(today);

  const { data: report, isLoading } = useQuery({
    queryKey: ['daily-report', user?.id, date],
    queryFn: () => getDailyReport(user!.id, date).then(r => r.data),
    enabled: !!user,
  });

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold text-text">Daily Report</h2>
        <input type="date" value={date} max={today} onChange={e => setDate(e.target.value)}
          className="bg-mantle border border-surface0 text-text rounded-lg px-3 py-2 text-sm focus:outline-none focus:border-blue" />
      </div>

      {isLoading && <div className="text-subtext0">Loading...</div>}
      {!isLoading && !report && <div className="text-subtext0 bg-mantle border border-surface0 rounded-xl p-8 text-center">No data for this date.</div>}

      {report && (
        <>
          <div className="grid grid-cols-3 gap-4">
            <div className="bg-mantle border border-surface0 rounded-xl p-4">
              <div className="text-2xl font-bold text-text">{formatDuration(report.totals.activeSeconds)}</div>
              <div className="text-xs text-subtext0 mt-1">Active Time</div>
            </div>
            <div className="bg-mantle border border-surface0 rounded-xl p-4">
              <div className="text-2xl font-bold text-text">{formatDuration(report.totals.idleSeconds)}</div>
              <div className="text-xs text-subtext0 mt-1">Idle Time</div>
            </div>
            <div className="bg-mantle border border-surface0 rounded-xl p-4">
              <div className="text-xs text-subtext0 mb-2">Productivity Score</div>
              <ProductivityScore score={report.totals.productivityScore} size="lg" />
            </div>
          </div>

          <div className="bg-mantle border border-surface0 rounded-xl p-5">
            <h3 className="text-sm font-semibold text-text mb-4">App Usage</h3>
            <AppUsagePieChart data={report.appBreakdown ?? []} />
          </div>

          {report.aiNarrative && (
            <div className="bg-mantle border border-surface0 rounded-xl p-5">
              <h3 className="text-sm font-semibold text-mauve mb-3">✨ AI Summary</h3>
              <p className="text-text text-sm leading-relaxed">{report.aiNarrative}</p>
            </div>
          )}
        </>
      )}
    </div>
  );
}
