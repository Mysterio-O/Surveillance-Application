import { useAuth } from '../../hooks/useAuth';

export default function ProfileSettingsPage() {
  const { user } = useAuth();
  return (
    <div className="space-y-4 max-w-md">
      <h2 className="text-xl font-semibold text-text">Profile</h2>
      <div className="bg-mantle border border-surface0 rounded-xl p-6 space-y-3">
        {[
          { label: 'Full Name', value: user?.fullName },
          { label: 'Email', value: user?.email },
          { label: 'Role', value: user?.role },
          { label: 'Organization', value: user?.orgName },
        ].map(f => (
          <div key={f.label} className="flex justify-between py-2 border-b border-surface0/50">
            <span className="text-subtext0 text-sm">{f.label}</span>
            <span className="text-text text-sm font-medium">{f.value}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
