import { useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useAuthStore } from '../../stores/authStore';
import { acceptInvite } from '../../api/auth';

export default function AcceptInvitePage() {
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') || '';
  const { login: storeLogin } = useAuthStore();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (password !== confirmPassword) { setError('Passwords do not match'); return; }
    setLoading(true);
    try {
      const res = await acceptInvite(token, password, confirmPassword);
      storeLogin(res.data.accessToken, res.data.refreshToken, res.data.user);
      navigate('/dashboard');
    } catch {
      setError('Invalid or expired invite token.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-base flex items-center justify-center">
      <div className="w-full max-w-sm">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-blue mb-2">SurveilWin</h1>
          <p className="text-subtext0">Set up your account</p>
        </div>
        <div className="bg-mantle rounded-xl border border-surface0 p-8">
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-xs text-subtext0 mb-1.5">New Password</label>
              <input type="password" value={password} onChange={e => setPassword(e.target.value)} required
                className="w-full bg-surface0 border border-surface1 text-text rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:border-blue" />
            </div>
            <div>
              <label className="block text-xs text-subtext0 mb-1.5">Confirm Password</label>
              <input type="password" value={confirmPassword} onChange={e => setConfirmPassword(e.target.value)} required
                className="w-full bg-surface0 border border-surface1 text-text rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:border-blue" />
            </div>
            {error && <p className="text-red text-xs">{error}</p>}
            <button type="submit" disabled={loading}
              className="w-full bg-blue hover:bg-blue/90 text-base font-semibold py-2.5 rounded-lg transition-colors disabled:opacity-50">
              {loading ? 'Setting up...' : 'Create Account'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
