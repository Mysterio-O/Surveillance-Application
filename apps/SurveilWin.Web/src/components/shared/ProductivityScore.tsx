interface Props { score: number | undefined; size?: 'sm' | 'md' | 'lg'; }

export function ProductivityScore({ score, size = 'md' }: Props) {
  if (score === undefined) return <span className="text-subtext0">—</span>;
  const pct = Math.round(score * 100);
  const color = pct >= 70 ? 'text-green' : pct >= 40 ? 'text-yellow' : 'text-red';
  const barColor = pct >= 70 ? 'bg-green' : pct >= 40 ? 'bg-yellow' : 'bg-red';
  const textSize = size === 'lg' ? 'text-2xl' : size === 'sm' ? 'text-xs' : 'text-sm';

  return (
    <div className="flex items-center gap-2">
      <div className="flex-1 bg-surface0 rounded-full h-1.5 min-w-[60px]">
        <div className={`h-1.5 rounded-full ${barColor}`} style={{ width: `${pct}%` }} />
      </div>
      <span className={`font-semibold ${color} ${textSize}`}>{pct}%</span>
    </div>
  );
}
