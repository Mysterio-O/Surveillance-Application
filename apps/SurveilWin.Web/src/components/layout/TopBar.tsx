import { useAuth } from '../../hooks/useAuth';

export function TopBar({ title }: { title?: string }) {
  const { user } = useAuth();
  return (
    <div className="h-14 bg-mantle border-b border-surface0 flex items-center justify-between px-6">
      <h1 className="text-text font-semibold text-base">{title}</h1>
      <div className="text-subtext0 text-sm">{user?.email}</div>
    </div>
  );
}
