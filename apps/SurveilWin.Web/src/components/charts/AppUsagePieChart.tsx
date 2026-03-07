import { PieChart, Pie, Cell, Tooltip, ResponsiveContainer } from 'recharts';
import type { AppDwellTime } from '../../types/activity';

const CATEGORY_COLORS: Record<string, string> = {
  coding:         '#a6e3a1',
  browser_work:   '#89b4fa',
  browser:        '#89dceb',
  communication:  '#cba6f7',
  docs:           '#f9e2af',
  terminal:       '#94e2d5',
  media:          '#f38ba8',
  system:         '#a6adc8',
  idle:           '#313244',
  other:          '#585b70',
};

interface Props { data: AppDwellTime[]; }

export function AppUsagePieChart({ data }: Props) {
  if (!data.length) return <div className="flex items-center justify-center h-48 text-subtext0">No data</div>;

  const chartData = data.map(d => ({
    name: d.displayName || d.app,
    value: d.seconds,
    category: d.category,
    percent: d.percent,
  }));

  return (
    <ResponsiveContainer width="100%" height={220}>
      <PieChart>
        <Pie data={chartData} cx="50%" cy="50%" outerRadius={80} dataKey="value" label={({ name, percent }) => `${name} ${((percent ?? 0) * 100).toFixed(0)}%`} labelLine={false}>
          {chartData.map((entry, i) => (
            <Cell key={i} fill={CATEGORY_COLORS[entry.category] ?? '#585b70'} />
          ))}
        </Pie>
        <Tooltip formatter={(val) => [`${Math.round((val as number) / 60)}m`, 'Time']} />
      </PieChart>
    </ResponsiveContainer>
  );
}
