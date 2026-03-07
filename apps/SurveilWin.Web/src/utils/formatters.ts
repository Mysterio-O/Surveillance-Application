export const formatDuration = (seconds: number): string => {
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  if (h > 0) return `${h}h ${m}m`;
  return `${m}m`;
};

export const formatPercent = (value: number): string =>
  `${Math.round(value * 100)}%`;

export const formatScore = (score: number | undefined): string =>
  score !== undefined ? `${Math.round(score * 100)}%` : '—';

export const formatHours = (hours: number | undefined): string =>
  hours !== undefined ? `${hours.toFixed(1)}h` : '—';
