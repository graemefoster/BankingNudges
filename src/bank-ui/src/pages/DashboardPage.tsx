import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import type { Account } from '../types';
import { formatCurrency } from '../types';
import { getCustomerAccounts } from '../api/bankApi';
import AccountCard from '../components/AccountCard';

export default function DashboardPage() {
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const customerId = sessionStorage.getItem('customerId');
  const customerName = sessionStorage.getItem('customerName');

  useEffect(() => {
    if (!customerId) {
      navigate('/');
      return;
    }
    getCustomerAccounts(customerId)
      .then(setAccounts)
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, [customerId, navigate]);

  const totalBalance = accounts.reduce((sum, a) => sum + a.balance, 0);

  return (
    <div>
      {/* Greeting */}
      <div className="mb-6">
        <p className="text-text-secondary text-sm">Welcome back,</p>
        <h2 className="text-xl font-bold text-text-primary">
          {customerName ?? 'Customer'}
        </h2>
      </div>

      {/* Total balance */}
      <div className="bg-brand-purple rounded-xl p-5 mb-6 text-center shadow-md">
        <p className="text-xs text-white/80 uppercase tracking-wide mb-1">
          Total Balance
        </p>
        <p className="text-3xl font-bold text-white">
          {loading ? '—' : formatCurrency(totalBalance)}
        </p>
      </div>

      {error && (
        <div className="bg-brand-red/10 border border-brand-red/30 text-brand-red rounded-lg px-4 py-3 mb-4 text-sm">
          {error}
        </div>
      )}

      {/* Accounts */}
      <h3 className="text-sm font-semibold text-text-secondary uppercase tracking-wide mb-3">
        Your Accounts
      </h3>

      {loading ? (
        <div className="flex justify-center py-12">
          <div className="w-8 h-8 border-2 border-brand-purple border-t-transparent rounded-full animate-spin" />
        </div>
      ) : (
        <div className="space-y-3">
          {accounts.map((a) => (
            <AccountCard key={a.id} account={a} />
          ))}
        </div>
      )}
    </div>
  );
}
