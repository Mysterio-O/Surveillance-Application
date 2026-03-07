import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { listUsers } from '../../api/users';
import { Plus } from 'lucide-react';

export default function EmployeeListPage() {
  const [search, setSearch] = useState('');
  const { data: users, isLoading } = useQuery({ queryKey: ['users'], queryFn: () => listUsers().then(r => r.data) });

  const filtered = users?.filter(u =>
    u.fullName.toLowerCase().includes(search.toLowerCase()) ||
    u.email.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold text-text">Team Members</h2>
        <Link to="/employees/invite"
          className="flex items-center gap-2 bg-blue hover:bg-blue/90 text-base text-sm font-medium px-4 py-2 rounded-lg transition-colors">
          <Plus size={14} /> Invite Employee
        </Link>
      </div>

      <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by name or email..."
        className="w-full bg-mantle border border-surface0 text-text rounded-lg px-3 py-2 text-sm focus:outline-none focus:border-blue" />

      <div className="bg-mantle border border-surface0 rounded-xl overflow-hidden">
        {isLoading ? (
          <div className="p-8 text-center text-subtext0">Loading...</div>
        ) : (
          <table className="w-full">
            <thead>
              <tr className="text-xs text-subtext0 border-b border-surface0">
                <th className="text-left px-4 py-3">Name</th>
                <th className="text-left px-4 py-3">Email</th>
                <th className="text-left px-4 py-3">Role</th>
                <th className="text-left px-4 py-3">Status</th>
                <th className="text-left px-4 py-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {(filtered ?? []).map(u => (
                <tr key={u.id} className="border-b border-surface0/50 hover:bg-surface0/30 transition-colors">
                  <td className="px-4 py-3 text-sm text-text font-medium">{u.fullName}</td>
                  <td className="px-4 py-3 text-sm text-subtext0">{u.email}</td>
                  <td className="px-4 py-3">
                    <span className="text-xs bg-surface0 text-text px-2 py-0.5 rounded-full">{u.role}</span>
                  </td>
                  <td className="px-4 py-3">
                    <span className={`text-xs ${u.isActive ? 'text-green' : 'text-red'}`}>
                      {u.isActive ? '🟢 Active' : '⚪ Inactive'}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <Link to={`/employees/${u.id}`} className="text-xs text-blue hover:text-blue/80 mr-3">View</Link>
                    <Link to={`/activity/${u.id}`} className="text-xs text-mauve hover:text-mauve/80">Activity</Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
