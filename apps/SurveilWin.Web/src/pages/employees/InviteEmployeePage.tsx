import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { inviteUser } from '../../api/users';

export default function InviteEmployeePage() {
  const navigate = useNavigate();
  const [form, setForm] = useState({ email: '', firstName: '', lastName: '', role: 'Employee' });
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      await inviteUser(form);
      toast.success(`Invite sent to ${form.email}`);
      navigate('/employees');
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message ?? 'Failed to send invite';
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-md space-y-6">
      <h2 className="text-xl font-semibold text-text">Invite Employee</h2>
      <div className="bg-mantle border border-surface0 rounded-xl p-6">
        <form onSubmit={handleSubmit} className="space-y-4">
          {[
            { label: 'First Name', key: 'firstName', type: 'text' },
            { label: 'Last Name', key: 'lastName', type: 'text' },
            { label: 'Email', key: 'email', type: 'email' },
          ].map(f => (
            <div key={f.key}>
              <label className="block text-xs text-subtext0 mb-1.5">{f.label}</label>
              <input type={f.type} required value={form[f.key as keyof typeof form]}
                onChange={e => setForm(prev => ({ ...prev, [f.key]: e.target.value }))}
                className="w-full bg-surface0 border border-surface1 text-text rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:border-blue" />
            </div>
          ))}
          <div>
            <label className="block text-xs text-subtext0 mb-1.5">Role</label>
            <select value={form.role} onChange={e => setForm(prev => ({ ...prev, role: e.target.value }))}
              className="w-full bg-surface0 border border-surface1 text-text rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:border-blue">
              <option value="Employee">Employee</option>
              <option value="Manager">Manager</option>
            </select>
          </div>
          <button type="submit" disabled={loading}
            className="w-full bg-blue hover:bg-blue/90 text-base font-semibold py-2.5 rounded-lg transition-colors disabled:opacity-50">
            {loading ? 'Sending...' : 'Send Invite'}
          </button>
        </form>
      </div>
    </div>
  );
}
