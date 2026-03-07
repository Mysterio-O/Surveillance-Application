import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../stores/authStore';
import { login } from '../../api/auth';

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const { login: storeLogin } = useAuthStore();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const res = await login(email, password);
      storeLogin(res.data.accessToken, res.data.refreshToken, res.data.user);
      navigate('/dashboard');
    } catch {
      setError('Invalid email or password.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-base flex items-center justify-center">
      <div className="w-full max-w-sm">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-blue mb-2">SurveilWin</h1>
          <p className="text-subtext0 text-sm">Employee Activity Platform</p>
        </div>
        <div className="bg-mantle rounded-xl border border-surface0 p-8">
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-xs text-subtext0 mb-1.5">Email</label>
              <input type="email" value={email} onChange={e => setEmail(e.target.value)} required
                className="w-full bg-surface0 border border-surface1 text-text rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:border-blue" />
            </div>
            <div>
              <label className="block text-xs text-subtext0 mb-1.5">Password</label>
              <input type="password" value={password} onChange={e => setPassword(e.target.value)} required
                className="w-full bg-surface0 border border-surface1 text-text rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:border-blue" />
            </div>
            {error && <p className="text-red text-xs">{error}</p>}
            <button type="submit" disabled={loading}
              className="w-full bg-blue hover:bg-blue/90 text-base font-semibold py-2.5 rounded-lg transition-colors disabled:opacity-50">
              {loading ? 'Signing in...' : 'Sign In'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
