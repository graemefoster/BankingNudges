import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { login } from '../api/crmApi';

export default function LoginPage() {
  const navigate = useNavigate();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const session = await login(username, password);
      sessionStorage.setItem('staff_session', JSON.stringify(session));
      navigate('/customers');
    } catch {
      setError('Invalid username or password');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="w-full max-w-xs">
      <fieldset>
        <legend>Staff Login</legend>

        {error && (
          <div className="mb-2 p-1.5 bg-red-100 text-red-700 text-xs border border-red-300">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-2">
          <div>
            <label className="block text-xs mb-0.5">Username</label>
            <input
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              className="w-full px-2 py-1 border border-border bg-white text-xs"
              required
            />
          </div>
          <div>
            <label className="block text-xs mb-0.5">Password</label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full px-2 py-1 border border-border bg-white text-xs"
              required
            />
          </div>
          <button
            type="submit"
            disabled={loading}
            className="w-full py-1.5 bg-crm-dark text-white text-xs font-bold hover:bg-crm-dark/90 disabled:opacity-50 border border-crm-dark cursor-pointer"
          >
            {loading ? 'Signing in...' : 'Sign In'}
          </button>
        </form>

        <p className="mt-2 text-[10px] text-text-secondary text-center">
          Demo credentials: admin/admin or teller/teller
        </p>
      </fieldset>
    </div>
  );
}
