import { useEffect, useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import type {
  NudgeInsightResponse,
  NudgeInsightSignal,
  NudgeInsightPayment,
  Account,
} from '../types';
import { formatCurrency, AccountType } from '../types';
import { getNudgeInsight, getCustomerAccounts } from '../api/bankApi';

/* ── helpers ────────────────────────────────────────────────────── */

function relativeTime(iso: string): string {
  const diffMs = Date.now() - new Date(iso).getTime();
  const diffMin = Math.floor(diffMs / 60000);
  if (diffMin < 1) return 'Just now';
  if (diffMin < 60) return `${diffMin}m ago`;
  const diffHr = Math.floor(diffMin / 60);
  if (diffHr < 24) return `${diffHr}h ago`;
  const diffDay = Math.floor(diffHr / 24);
  if (diffDay === 1) return 'Yesterday';
  if (diffDay < 7) return `${diffDay}d ago`;
  return new Date(iso).toLocaleDateString('en-AU', { day: 'numeric', month: 'short' });
}

const urgencyConfig: Record<string, { border: string; bg: string; icon: string }> = {
  HIGH: { border: 'border-accent-coral/40', bg: 'bg-accent-coral/10', icon: '🔴' },
  MEDIUM: { border: 'border-accent-amber/40', bg: 'bg-accent-amber/10', icon: '🟡' },
  LOW: { border: 'border-accent-teal/40', bg: 'bg-accent-teal/10', icon: '🟢' },
};

const severityColor: Record<string, string> = {
  HIGH: 'bg-accent-coral/15 text-accent-coral',
  MEDIUM: 'bg-accent-amber/15 text-accent-amber',
  LOW: 'bg-accent-teal/15 text-accent-teal',
};

const signalLabel: Record<string, string> = {
  LOW_BALANCE: 'Low Balance',
  CANT_COVER_UPCOMING: "Can't Cover Bills",
  PAYMENT_DUE_SOON: 'Payment Due Soon',
  SPEND_SPIKE: 'Spend Spike',
  EXCESS_CASH_SITTING: 'Excess Cash',
  PAYDAY_INCOMING: 'Payday Soon',
};

const categoryEmoji: Record<string, string> = {
  SPENDING: '💳',
  CASHFLOW: '💰',
  UPCOMING_PAYMENT: '📅',
  SAVINGS: '🏦',
};

/* ── page component ─────────────────────────────────────────────── */

export default function NudgeInsightPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [data, setData] = useState<NudgeInsightResponse | null>(null);
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [reasoningOpen, setReasoningOpen] = useState(false);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    const customerId = sessionStorage.getItem('customerId');
    Promise.all([
      getNudgeInsight(Number(id)),
      customerId ? getCustomerAccounts(customerId) : Promise.resolve([]),
    ])
      .then(([insight, accts]) => {
        setData(insight);
        setAccounts(accts);
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, [id]);

  /* Loading */
  if (loading) {
    return (
      <div className="flex justify-center py-16">
        <div className="w-8 h-8 border-2 border-accent-teal border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  /* Error / 404 */
  if (error || !data) {
    return (
      <div className="text-center py-16">
        <div className="text-5xl mb-4">🔍</div>
        <p className="text-accent-coral mb-4">{error || 'Insight not found'}</p>
        <Link to="/dashboard" className="text-accent-teal hover:underline text-sm font-medium">
          Back to Dashboard
        </Link>
      </div>
    );
  }

  const { nudge, context } = data;
  const config = urgencyConfig[nudge.urgency] ?? urgencyConfig.MEDIUM;
  const primaryAccount = accounts.find((a) => a.accountType === AccountType.Transaction) ?? accounts[0];

  return (
    <div>
      {/* Back link */}
      <button
        onClick={() => navigate(-1)}
        className="inline-flex items-center text-sm text-accent-teal hover:text-accent-teal/70 mb-4 transition-colors font-medium"
      >
        ← Back
      </button>

      {/* ── Nudge message card ─────────────────────────────────── */}
      <div className={`rounded-xl border ${config.border} ${config.bg} p-4 mb-6`}>
        <div className="flex items-start gap-3 mb-3">
          <span className="text-lg">{config.icon}</span>
          <p className="text-sm text-text-primary leading-relaxed flex-1">{nudge.message}</p>
        </div>
        <div className="flex items-center gap-2">
          <span className="text-xs font-medium px-2 py-0.5 rounded-full bg-dark-surface text-text-secondary">
            {nudge.category.replace('_', ' ')}
          </span>
          <span className="text-xs text-text-muted">Generated {relativeTime(nudge.createdAt)}</span>
        </div>
      </div>

      {/* ── Category-specific insight ──────────────────────────── */}
      {nudge.category === 'SPENDING' && (
        <SpendingSection context={context} accountId={primaryAccount?.id} />
      )}
      {nudge.category === 'CASHFLOW' && (
        <CashflowSection context={context} />
      )}
      {nudge.category === 'UPCOMING_PAYMENT' && (
        <UpcomingPaymentSection context={context} accountId={primaryAccount?.id} />
      )}
      {nudge.category === 'SAVINGS' && (
        <SavingsSection context={context} />
      )}

      {/* Show upcoming payments for any category that has them (unless already the primary section) */}
      {nudge.category !== 'UPCOMING_PAYMENT' && context.upcoming.length > 0 && (
        <UpcomingPaymentSection context={context} accountId={primaryAccount?.id} />
      )}

      {/* ── Why this insight ───────────────────────────────────── */}
      <div className="bg-dark-elevated rounded-xl border border-border mb-6">
        <button
          onClick={() => setReasoningOpen((v) => !v)}
          className="w-full flex items-center justify-between px-4 py-3 text-sm font-semibold text-text-primary"
        >
          <span>Why this insight</span>
          <span className="text-text-muted">{reasoningOpen ? '▴' : '▾'}</span>
        </button>
        {reasoningOpen && (
          <div className="px-4 pb-4">
            <p className="text-sm text-text-secondary leading-relaxed mb-3">
              {nudge.reasoning}
            </p>
            <div className="flex flex-wrap gap-2">
              {context.signals.map((s, i) => (
                <span
                  key={i}
                  className={`text-xs font-medium px-2 py-0.5 rounded-full ${severityColor[s.severity] ?? severityColor.MEDIUM}`}
                >
                  {signalLabel[s.type] ?? s.type}
                </span>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

/* ── SPENDING section ───────────────────────────────────────────── */

function SpendingSection({ context, accountId }: { context: NudgeInsightResponse['context']; accountId?: string }) {
  const navigate = useNavigate();
  const { financial, signals } = context;
  const entries = Object.entries(financial.spendByCategory).sort(([, a], [, b]) => b - a);
  const maxAmount = entries.length > 0 ? entries[0][1] : 1;
  const spikeCategories = new Set(
    signals.filter((s) => s.type === 'SPEND_SPIKE' && s.category).map((s) => s.category!),
  );

  return (
    <div className="mb-6">
      <h3 className="text-base font-semibold text-text-primary mb-3 flex items-center gap-2">
        <span>{categoryEmoji.SPENDING}</span> Spending Breakdown
      </h3>
      <div className="bg-dark-elevated rounded-xl border border-border p-4 space-y-3">
        {entries.map(([cat, amount]) => {
          const pct = (amount / maxAmount) * 100;
          const delta = financial.spendDelta[cat];
          const isSpike = spikeCategories.has(cat);

          return (
            <button
              key={cat}
              className="w-full text-left group"
              onClick={() => accountId && navigate(`/accounts/${accountId}?category=${encodeURIComponent(cat)}`)}
              disabled={!accountId}
            >
              <div className="flex items-center justify-between mb-1">
                <span className={`text-sm ${isSpike ? 'text-accent-coral font-semibold' : 'text-text-primary group-hover:text-accent-teal'}`}>
                  {cat}
                  {accountId && <span className="text-text-muted text-xs ml-1 opacity-0 group-hover:opacity-100 transition-opacity">→</span>}
                </span>
                <div className="flex items-center gap-2">
                  {delta != null && delta !== 0 && (
                    <span
                      className={`text-xs font-medium px-1.5 py-0.5 rounded ${
                        delta > 0
                          ? 'bg-accent-coral/15 text-accent-coral'
                          : 'bg-accent-teal/15 text-accent-teal'
                      }`}
                    >
                      {delta > 0 ? '↑' : '↓'}
                      {Math.abs(Math.round(delta * 100))}%
                    </span>
                  )}
                  <span className="text-sm text-text-secondary font-medium tabular-nums">
                    {formatCurrency(amount)}
                  </span>
                </div>
              </div>
              <div className="h-2 rounded-full bg-dark-surface overflow-hidden">
                <div
                  className={`h-full rounded-full transition-all ${isSpike ? 'bg-accent-coral' : 'bg-accent-teal'}`}
                  style={{ width: `${pct}%` }}
                />
              </div>
            </button>
          );
        })}
        {entries.length === 0 && (
          <p className="text-sm text-text-muted text-center py-2">No spending data</p>
        )}
      </div>
    </div>
  );
}

/* ── CASHFLOW section ───────────────────────────────────────────── */

function CashflowSection({ context }: { context: NudgeInsightResponse['context'] }) {
  const { financial } = context;
  const estimatedExpenses = Object.values(financial.spendByCategory).reduce((s, v) => s + v, 0);
  const isPositive = financial.currentBalance >= 0;

  return (
    <div className="mb-6">
      <h3 className="text-base font-semibold text-text-primary mb-3 flex items-center gap-2">
        <span>{categoryEmoji.CASHFLOW}</span> Cash Flow Summary
      </h3>
      <div className="bg-dark-elevated rounded-xl border border-border p-4 space-y-4">
        {/* Balance */}
        <div className="text-center">
          <p className="text-xs text-text-muted uppercase tracking-wide mb-1">Current Balance</p>
          <p className={`text-3xl font-extrabold tracking-tight ${isPositive ? 'text-accent-teal' : 'text-accent-coral'}`}>
            {formatCurrency(financial.currentBalance)}
          </p>
        </div>

        {/* Income vs Expenses */}
        <div className="grid grid-cols-2 gap-3">
          <div className="bg-dark-surface rounded-lg p-3 text-center">
            <p className="text-xs text-text-muted mb-1">Monthly Income</p>
            <p className="text-sm font-bold text-accent-teal">{formatCurrency(financial.avgMonthlyIncome)}</p>
          </div>
          <div className="bg-dark-surface rounded-lg p-3 text-center">
            <p className="text-xs text-text-muted mb-1">Est. Expenses</p>
            <p className="text-sm font-bold text-accent-coral">{formatCurrency(estimatedExpenses)}</p>
          </div>
        </div>

        {/* Payday */}
        <div className="flex items-center gap-3 bg-dark-surface rounded-lg p-3">
          <span className="text-lg">📆</span>
          <div>
            <p className="text-sm text-text-primary font-medium">
              {financial.daysUntilLikelyPayday === 0
                ? 'Payday is today!'
                : financial.daysUntilLikelyPayday === 1
                  ? 'Payday tomorrow'
                  : `${financial.daysUntilLikelyPayday} days until likely payday`}
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}

/* ── UPCOMING_PAYMENT section ───────────────────────────────────── */

function UpcomingPaymentSection({ context, accountId }: { context: NudgeInsightResponse['context']; accountId?: string }) {
  const { upcoming, signals } = context;

  const triggeredMerchants = new Set(
    signals
      .filter((s) => s.type === 'PAYMENT_DUE_SOON' && s.paymentMerchant)
      .map((s) => s.paymentMerchant!),
  );

  const sorted = [...upcoming].sort((a, b) => a.dueInDays - b.dueInDays);

  return (
    <div className="mb-6">
      <h3 className="text-base font-semibold text-text-primary mb-3 flex items-center gap-2">
        <span>{categoryEmoji.UPCOMING_PAYMENT}</span> Upcoming Payments
      </h3>
      <div className="bg-dark-elevated rounded-xl border border-border p-4">
        {sorted.length === 0 ? (
          <p className="text-sm text-text-muted text-center py-2">No upcoming payments</p>
        ) : (
          <div className="space-y-3">
            {sorted.map((p, i) => (
              <PaymentRow
                key={i}
                payment={p}
                highlighted={triggeredMerchants.has(p.merchant)}
                accountId={accountId}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

function PaymentRow({ payment, highlighted, accountId }: { payment: NudgeInsightPayment; highlighted: boolean; accountId?: string }) {
  const navigate = useNavigate();
  const dueLabel =
    payment.dueInDays === 0
      ? 'Due today'
      : payment.dueInDays === 1
        ? 'Due tomorrow'
        : `Due in ${payment.dueInDays} days`;

  const sourceStyle =
    payment.source === 'Scheduled'
      ? 'bg-accent-teal/15 text-accent-teal'
      : 'bg-accent-amber/15 text-accent-amber';

  const isInferred = payment.source === 'InferredPattern';
  const canLink = isInferred && accountId;

  const handleClick = () => {
    if (canLink) {
      navigate(`/accounts/${accountId}?search=${encodeURIComponent(payment.merchant)}`);
    }
  };

  return (
    <div
      role={canLink ? 'button' : undefined}
      tabIndex={canLink ? 0 : undefined}
      onClick={handleClick}
      onKeyDown={canLink ? (e) => { if (e.key === 'Enter' || e.key === ' ') handleClick(); } : undefined}
      className={`flex items-center gap-3 rounded-lg p-3 ${
        highlighted ? 'bg-accent-coral/5 border border-accent-coral/30' : 'bg-dark-surface'
      } ${canLink ? 'cursor-pointer hover:ring-1 hover:ring-accent-teal/40 transition-all' : ''}`}
    >
      {/* Timeline dot */}
      <div className="flex flex-col items-center shrink-0">
        <div className={`w-2.5 h-2.5 rounded-full ${highlighted ? 'bg-accent-coral' : 'bg-accent-teal'}`} />
      </div>

      {/* Details */}
      <div className="flex-1 min-w-0">
        <p className={`text-sm font-medium truncate ${highlighted ? 'text-accent-coral' : 'text-text-primary'} ${canLink ? 'group-hover:text-accent-teal' : ''}`}>
          {payment.merchant}
          {canLink && <span className="text-text-muted text-xs ml-1">→ View transactions</span>}
        </p>
        <div className="flex items-center gap-2 mt-0.5">
          <span className="text-xs text-text-muted">{dueLabel}</span>
          <span className={`text-xs font-medium px-1.5 py-0.5 rounded-full ${sourceStyle}`}>
            {payment.source}
          </span>
        </div>
      </div>

      {/* Amount */}
      <span className="text-sm font-bold text-text-primary tabular-nums shrink-0">
        {formatCurrency(payment.amount)}
      </span>
    </div>
  );
}

/* ── SAVINGS section ────────────────────────────────────────────── */

function SavingsSection({ context }: { context: NudgeInsightResponse['context'] }) {
  const { financial } = context;
  const accounts = financial.accounts ?? [];

  return (
    <div className="mb-6">
      <h3 className="text-base font-semibold text-text-primary mb-3 flex items-center gap-2">
        <span>{categoryEmoji.SAVINGS}</span> Your Accounts
      </h3>
      <div className="bg-dark-elevated rounded-xl border border-border p-4 space-y-3">
        {accounts.length > 0 ? (
          accounts.map((acct) => {
            const isOffset = acct.accountType === 'Offset' && acct.offsetHomeLoanRate != null;

            return (
              <div key={acct.name} className="flex items-center justify-between py-2 border-b border-border/40 last:border-b-0">
                <div>
                  <p className="text-sm font-medium text-text-primary">{acct.name}</p>
                  <p className="text-xs text-text-muted">
                    {isOffset
                      ? `Offset · ${acct.offsetHomeLoanRate!.toFixed(2)}% home loan rate`
                      : <>
                          {acct.accountType}
                          {acct.interestRate != null && ` · ${acct.interestRate.toFixed(2)}% p.a.`}
                          {acct.bonusInterestRate != null && ` + ${acct.bonusInterestRate.toFixed(2)}% bonus`}
                        </>
                    }
                  </p>
                </div>
                <p className="text-sm font-semibold text-text-primary tabular-nums">
                  {formatCurrency(acct.balance)}
                </p>
              </div>
            );
          })
        ) : (
          <p className="text-sm text-text-muted text-center py-2">
            Total balance: {formatCurrency(financial.currentBalance)}
          </p>
        )}

        {/* Disclaimer */}
        <p className="text-xs text-text-muted/70 leading-relaxed pt-2 border-t border-border/50">
          This is general information only, not personal financial advice. Rates may change. Consider your personal circumstances, including tax implications, before making financial decisions.
        </p>
      </div>
    </div>
  );
}
