import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import type { Account, NudgeGenerateResult } from '../types';
import { formatCurrency } from '../types';
import { getCustomerAccounts, generateNudge } from '../api/bankApi';
import AccountCard from '../components/AccountCard';
import NudgeCard from '../components/NudgeCard';

export default function DashboardPage() {
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [nudgeResult, setNudgeResult] = useState<NudgeGenerateResult | null>(null);
  const [nudgeLoading, setNudgeLoading] = useState(false);
  const [nudgeDismissed, setNudgeDismissed] = useState(false);
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

  const handleGenerateNudge = async () => {
    if (!customerId || nudgeLoading) return;
    setNudgeLoading(true);
    setNudgeDismissed(false);
    setNudgeResult(null);
    try {
      const result = await generateNudge(customerId);
      setNudgeResult(result);
    } catch (e: unknown) {
      setNudgeResult({ generated: false, nudge: null, reason: e instanceof Error ? e.message : 'Something went wrong' });
    } finally {
      setNudgeLoading(false);
    }
  };

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
      <div className="bg-gradient-to-br from-accent-teal to-accent-cyan rounded-2xl p-5 mb-6 text-center">
        <p className="text-xs text-white/80 uppercase tracking-wide mb-1">
          Total Balance
        </p>
        <p className="text-3xl font-extrabold text-white tracking-tight">
          {loading ? '—' : formatCurrency(totalBalance)}
        </p>
      </div>

      {/* Nudge section */}
      <div className="mb-6">
        {nudgeDismissed ? (
          <div className="text-center text-xs text-text-secondary py-2">
            ✓ Got it
          </div>
        ) : nudgeLoading ? (
          <div className="flex items-center justify-center gap-2 py-4">
            <div className="w-5 h-5 border-2 border-accent-teal border-t-transparent rounded-full animate-spin" />
            <span className="text-xs text-text-secondary">Checking your finances…</span>
          </div>
        ) : nudgeResult?.generated && nudgeResult.nudge ? (
          <NudgeCard
            nudge={nudgeResult.nudge}
            onDismissed={() => setNudgeDismissed(true)}
          />
        ) : nudgeResult && !nudgeResult.generated ? (
          <div className="text-center text-sm text-text-secondary py-3 bg-dark-elevated rounded-xl border border-border">
            {nudgeResult.reason ?? 'No insights right now'}
          </div>
        ) : (
          <button
            onClick={handleGenerateNudge}
            className="w-full flex items-center justify-center gap-2 py-3 rounded-xl bg-dark-elevated border border-border hover:border-accent-teal/40 transition-colors"
          >
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" className="text-accent-teal">
              <path d="M12 2l2.4 7.2H22l-6 4.8 2.4 7.2L12 16.4 5.6 21.2 8 14 2 9.2h7.6z" />
            </svg>
            <span className="text-sm font-medium text-text-secondary">Get a financial insight</span>
          </button>
        )}
      </div>

      {error && (
        <div className="bg-accent-coral/10 border border-accent-coral/30 text-accent-coral rounded-lg px-4 py-3 mb-4 text-sm">
          {error}
        </div>
      )}

      {/* Accounts */}
      <h3 className="text-sm font-semibold text-text-secondary uppercase tracking-wide mb-3">
        Your Accounts
      </h3>

      {loading ? (
        <div className="flex justify-center py-12">
          <div className="w-8 h-8 border-2 border-accent-teal border-t-transparent rounded-full animate-spin" />
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
