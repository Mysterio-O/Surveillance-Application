export function ShiftStatusBadge({ status }: { status: string }) {
  const styles: Record<string, string> = {
    Active:     'text-green',
    Completed:  'text-blue',
    AutoClosed: 'text-yellow',
  };
  const icons: Record<string, string> = {
    Active: '🟢', Completed: '✅', AutoClosed: '⏰',
  };
  return (
    <span className={`text-sm font-medium ${styles[status] ?? 'text-subtext0'}`}>
      {icons[status] ?? '⚪'} {status}
    </span>
  );
}
