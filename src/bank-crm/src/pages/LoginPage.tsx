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
    <div className="w-full max-w-sm">
      <div className="bg-crm-card rounded-xl shadow-lg p-8">
        <h2 className="text-xl font-bold text-crm-dark mb-1">Staff Login</h2>
        <p className="text-sm text-text-secondary mb-6">Sign in to the CRM system</p>

        {error && (
          <div className="mb-4 p-3 bg-crm-warning/10 text-crm-warning text-sm rounded-lg">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-text-primary mb-1">Username</label>
            <input
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-crm-accent focus:border-transparent"
              required
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-text-primary mb-1">Password</label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-crm-accent focus:border-transparent"
              required
            />
          </div>
          <button
            type="submit"
            disabled={loading}
            className="w-full py-2.5 bg-crm-accent text-crm-dark font-semibold rounded-lg hover:bg-crm-accent/90 transition-colors disabled:opacity-50"
          >
            {loading ? 'Signing in...' : 'Sign In'}
          </button>
        </form>

        <p className="mt-4 text-xs text-text-secondary text-center">
          Demo: admin/admin or teller/teller
        </p>
      </div>
    </div>
  );
}
