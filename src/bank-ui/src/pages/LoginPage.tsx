import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import type { Customer } from '../types';
import { getCustomers } from '../api/bankApi';

export default function LoginPage() {
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const navigate = useNavigate();

  useEffect(() => {
    getCustomers()
      .then(setCustomers)
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  function selectCustomer(c: Customer) {
    sessionStorage.setItem('customerId', c.id);
    sessionStorage.setItem('customerName', c.fullName);
    navigate('/dashboard');
  }

  return (
    <div className="flex flex-col items-center pt-12">
      {/* Logo icon */}
      <div className="w-16 h-16 rounded-2xl bg-gradient-to-br from-accent-teal to-accent-cyan flex items-center justify-center mb-5">
        <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
          <path d="M3 21h18" />
          <path d="M3 10h18" />
          <path d="M5 6l7-3 7 3" />
          <path d="M4 10v11" />
          <path d="M20 10v11" />
          <path d="M8 14v4" />
          <path d="M12 14v4" />
          <path d="M16 14v4" />
        </svg>
      </div>
      <h2 className="text-2xl font-bold bg-gradient-to-r from-accent-teal to-accent-cyan bg-clip-text text-transparent mb-1">
        Bank of Graeme
      </h2>
      <p className="text-text-secondary text-sm mb-8">Select your account to continue</p>

      {error && (
        <div className="w-full bg-accent-coral/10 border border-accent-coral/30 text-accent-coral rounded-lg px-4 py-3 mb-4 text-sm">
          {error}
        </div>
      )}

      {loading ? (
        <div className="flex justify-center py-12">
          <div className="w-8 h-8 border-2 border-accent-teal border-t-transparent rounded-full animate-spin" />
        </div>
      ) : (
        <div className="w-full space-y-3">
          {customers.map((c) => (
            <button
              key={c.id}
              onClick={() => selectCustomer(c)}
              className="w-full bg-dark-elevated hover:bg-dark-elevated/80 rounded-xl p-4 text-left transition-colors border border-border"
            >
              <div className="flex items-center justify-between">
                <div>
                  <p className="font-semibold text-text-primary">{c.fullName}</p>
                  <p className="text-xs text-text-secondary mt-0.5">{c.email}</p>
                </div>
                <div className="text-right">
                  <span className="text-xs bg-accent-teal/15 text-accent-teal px-2 py-1 rounded-full font-medium">
                    {c.accountCount} account{c.accountCount !== 1 ? 's' : ''}
                  </span>
                </div>
              </div>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
