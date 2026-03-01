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
      <div className="text-5xl mb-4">🏦</div>
      <h2 className="text-2xl font-bold text-brand-purple mb-1">
        Bank of Graeme
      </h2>
      <p className="text-text-secondary text-sm mb-8">Select your account to continue</p>

      {error && (
        <div className="w-full bg-brand-red/10 border border-brand-red/30 text-brand-red rounded-lg px-4 py-3 mb-4 text-sm">
          {error}
        </div>
      )}

      {loading ? (
        <div className="flex justify-center py-12">
          <div className="w-8 h-8 border-2 border-brand-purple border-t-transparent rounded-full animate-spin" />
        </div>
      ) : (
        <div className="w-full space-y-3">
          {customers.map((c) => (
            <button
              key={c.id}
              onClick={() => selectCustomer(c)}
              className="w-full bg-light-card hover:bg-light-card-hover rounded-xl p-4 text-left transition-colors shadow-sm"
            >
              <div className="flex items-center justify-between">
                <div>
                  <p className="font-semibold text-text-primary">{c.fullName}</p>
                  <p className="text-xs text-text-secondary mt-0.5">{c.email}</p>
                </div>
                <div className="text-right">
                  <span className="text-xs bg-brand-purple/15 text-brand-purple px-2 py-1 rounded-full font-medium">
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
