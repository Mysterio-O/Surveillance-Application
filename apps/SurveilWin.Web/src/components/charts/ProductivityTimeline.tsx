import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';

interface Props { data: { hour: number; score: number }[]; }

export function ProductivityTimeline({ data }: Props) {
  if (!data.length) return <div className="flex items-center justify-center h-48 text-subtext0">No data</div>;

  const chartData = data.map(d => ({
    hour: `${d.hour}:00`,
    score: Math.round(d.score * 100),
  }));

  return (
    <ResponsiveContainer width="100%" height={200}>
      <LineChart data={chartData}>
        <CartesianGrid strokeDasharray="3 3" stroke="#313244" />
        <XAxis dataKey="hour" stroke="#a6adc8" tick={{ fontSize: 11 }} />
        <YAxis domain={[0, 100]} stroke="#a6adc8" tick={{ fontSize: 11 }} unit="%" />
        <Tooltip contentStyle={{ background: '#181825', border: '1px solid #313244', color: '#cdd6f4' }}
                 formatter={(val) => [`${val as number}%`, 'Productivity']} />
        <Line type="monotone" dataKey="score" stroke="#89b4fa" strokeWidth={2} dot={false} />
      </LineChart>
    </ResponsiveContainer>
  );
}
