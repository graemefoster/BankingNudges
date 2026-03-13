import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import type { Account, NudgeGenerateResult, ActiveHoliday } from '../types';
import { formatCurrency } from '../types';
import { getCustomerAccounts, generateNudge, getActiveHoliday } from '../api/bankApi';
import AccountCard from '../components/AccountCard';
import NudgeCard from '../components/NudgeCard';
import TravelBanner from '../components/TravelBanner';
import ChatDrawer from '../components/ChatDrawer';

export default function DashboardPage() {
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [nudgeResult, setNudgeResult] = useState<NudgeGenerateResult | null>(null);
  const [nudgeLoading, setNudgeLoading] = useState(true);
  const [nudgeDismissed, setNudgeDismissed] = useState(false);
  const [activeHoliday, setActiveHoliday] = useState<ActiveHoliday | null>(null);
  const [chatOpen, setChatOpen] = useState(false);
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
    getActiveHoliday(customerId)
      .then(setActiveHoliday)
      .catch(() => { /* holiday banner is non-critical */ });
    generateNudge(customerId)
      .then(setNudgeResult)
      .catch((e: unknown) => setNudgeResult({ generated: false, nudge: null, reason: e instanceof Error ? e.message : 'Something went wrong' }))
      .finally(() => setNudgeLoading(false));
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

      {/* Travel banner — shown when the customer has a registered holiday today */}
      {activeHoliday && <TravelBanner holiday={activeHoliday} />}

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
            <span className="text-xs text-text-secondary">Getting financial insights...</span>
          </div>
        ) : nudgeResult?.generated && nudgeResult.nudge ? (
          <NudgeCard
            nudge={nudgeResult.nudge}
            onDismissed={() => setNudgeDismissed(true)}
            onChat={() => setChatOpen(true)}
          />
        ) : nudgeResult && !nudgeResult.generated ? (
          <div className={`text-center text-sm py-3 rounded-xl border ${
            nudgeResult.reason?.includes('⚠️')
              ? 'text-amber-400 bg-amber-950/30 border-amber-500/30'
              : 'text-text-secondary bg-dark-elevated border-border'
          }`}>
            {nudgeResult.reason ?? 'No insights right now'}
          </div>
        ) : null}
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
      {chatOpen && nudgeResult?.nudge && customerId && (
        <ChatDrawer
          nudge={nudgeResult.nudge}
          customerId={Number(customerId)}
          onClose={() => setChatOpen(false)}
        />
      )}
    </div>
  );
}
