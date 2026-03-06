import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import type { Account, ScheduledPayment } from '../types';
import {
  AccountType,
  accountTypeLabel,
  formatCurrency,
  frequencyLabel,
} from '../types';
import {
  getCustomerAccounts,
  getScheduledPayments,
  createScheduledPayment,
  cancelScheduledPayment,
} from '../api/bankApi';

export default function ScheduledPaymentsPage() {
  const [allAccounts, setAllAccounts] = useState<Account[]>([]);
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [payments, setPayments] = useState<Record<string, ScheduledPayment[]>>({});
  const [loading, setLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const navigate = useNavigate();

  const customerId = sessionStorage.getItem('customerId');

  async function loadAll() {
    if (!customerId) {
      navigate('/');
      return;
    }
    setLoading(true);
    try {
      const accs = await getCustomerAccounts(customerId, true);
      setAllAccounts(accs);
      const createEligible = accs.filter((a) => a.accountType !== AccountType.HomeLoan && a.isActive !== false);
      setAccounts(createEligible);

      const allPayments: Record<string, ScheduledPayment[]> = {};
      await Promise.all(
        accs.map(async (a) => {
          const sp = await getScheduledPayments(a.id);
          if (sp.length > 0) allPayments[String(a.id)] = sp;
        }),
      );
      setPayments(allPayments);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Failed to load');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadAll();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [customerId, navigate]);

  async function handleCancel(id: number) {
    try {
      await cancelScheduledPayment(id);
      setSuccess('Payment cancelled');
      setTimeout(() => setSuccess(''), 2000);
      await loadAll();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Failed to cancel');
    }
  }

  if (loading) {
    return (
      <div className="flex justify-center py-16">
        <div className="w-8 h-8 border-2 border-accent-teal border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  const allPayments = Object.entries(payments);
  const hasPayments = allPayments.some(([, sp]) => sp.length > 0);

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-xl font-bold text-text-primary">Scheduled Payments</h2>
        <button
          onClick={() => setShowCreate(true)}
          disabled={accounts.length === 0}
          className="bg-gradient-to-r from-accent-teal to-accent-cyan text-white text-sm font-semibold px-4 py-2 rounded-lg hover:opacity-90 transition-opacity"
        >
          + New
        </button>
      </div>

      {error && (
        <div className="bg-accent-coral/10 border border-accent-coral/30 text-accent-coral rounded-lg px-4 py-3 mb-4 text-sm">
          {error}
        </div>
      )}
      {success && (
        <div className="bg-accent-teal/10 border border-accent-teal/30 text-accent-teal rounded-lg px-4 py-3 mb-4 text-sm">
          {success}
        </div>
      )}
      {accounts.length === 0 && (
        <div className="bg-dark-surface border border-border text-text-secondary rounded-lg px-4 py-3 mb-4 text-sm">
          No eligible active source accounts available for creating new scheduled payments.
        </div>
      )}

      {!hasPayments ? (
        <div className="text-center py-16">
          <div className="text-4xl mb-3">📅</div>
          <p className="text-text-secondary mb-1">No scheduled payments yet</p>
          <p className="text-text-muted text-sm">Set up recurring payments or schedule a future payment</p>
        </div>
      ) : (
        <div className="space-y-6">
          {allPayments.map(([accountId, sp]) => {
            const account = allAccounts.find((a) => String(a.id) === accountId);
            if (!account || sp.length === 0) return null;
            return (
              <div key={accountId}>
                <div className="text-xs text-text-secondary mb-2 font-medium">
                  {account.name} ({accountTypeLabel[account.accountType]})
                  {account.isActive === false ? ' — Closed' : ''}
                </div>
                <div className="space-y-2">
                  {sp.map((p) => (
                    <div
                      key={p.id}
                      className={`bg-dark-surface rounded-xl p-4 border border-border ${!p.isActive ? 'opacity-50' : ''}`}
                    >
                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <p className="text-text-primary font-semibold">{p.payeeName}</p>
                          <p className="text-accent-coral font-mono text-lg font-bold mt-0.5">
                            {formatCurrency(p.amount)}
                          </p>
                        </div>
                        <div className="text-right">
                          <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-medium ${
                            p.isActive
                              ? 'bg-accent-teal/15 text-accent-teal'
                              : 'bg-dark-elevated text-text-muted'
                          }`}>
                            {p.isActive ? 'Active' : 'Cancelled'}
                          </span>
                        </div>
                      </div>
                      <div className="flex items-center gap-3 mt-3 text-xs text-text-secondary">
                        <span className="bg-dark-elevated px-2 py-0.5 rounded">
                          {frequencyLabel[p.frequency] ?? p.frequency}
                        </span>
                        <span>
                          Next: <span className="text-text-primary">{p.isActive ? formatDateShort(p.nextDueDate) : '—'}</span>
                        </span>
                        {p.endDate && (
                          <span>
                            Until: <span className="text-text-primary">{formatDateShort(p.endDate)}</span>
                          </span>
                        )}
                      </div>
                      {p.isActive && (
                        <button
                          onClick={() => handleCancel(p.id)}
                          className="mt-3 text-xs text-accent-coral hover:text-accent-coral/80 transition-colors"
                        >
                          Cancel payment
                        </button>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {showCreate && (
        <CreateScheduledPaymentModal
          accounts={accounts}
          onClose={() => setShowCreate(false)}
          onCreated={() => {
            setShowCreate(false);
            setSuccess('Scheduled payment created');
            setTimeout(() => setSuccess(''), 2000);
            loadAll();
          }}
        />
      )}
    </div>
  );
}

function formatDateShort(dateStr: string): string {
  const d = new Date(dateStr + 'T00:00:00');
  return d.toLocaleDateString('en-AU', { day: 'numeric', month: 'short', year: 'numeric' });
}

function CreateScheduledPaymentModal({
  accounts,
  onClose,
  onCreated,
}: {
  accounts: Account[];
  onClose: () => void;
  onCreated: () => void;
}) {
  const [accountId, setAccountId] = useState(accounts[0]?.id ?? '');
  const [payeeName, setPayeeName] = useState('');
  const [amount, setAmount] = useState('');
  const [description, setDescription] = useState('');
  const [frequency, setFrequency] = useState('Monthly');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');

  const inputClasses =
    'w-full bg-dark-elevated text-text-primary rounded-lg px-4 py-3 border border-border focus:border-accent-teal focus:ring-2 focus:ring-border-focus focus:outline-none';

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');

    if (!payeeName.trim()) { setError('Enter a payee name'); return; }
    const amt = parseFloat(amount);
    if (isNaN(amt) || amt <= 0) { setError('Enter a valid amount'); return; }
    if (!startDate) { setError('Select a start date'); return; }

    setSubmitting(true);
    try {
      await createScheduledPayment({
        accountId: Number(accountId),
        payeeName: payeeName.trim(),
        amount: amt,
        description: description.trim() || undefined,
        frequency,
        startDate,
        endDate: endDate || undefined,
      });
      onCreated();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="fixed inset-0 bg-black/60 flex items-end justify-center z-50" onClick={onClose}>
      <div
        className="bg-dark-bg rounded-t-2xl w-full max-w-md p-6 max-h-[85vh] overflow-y-auto"
        onClick={(e) => e.stopPropagation()}
      >
        <h3 className="text-lg font-bold text-text-primary mb-4">New Scheduled Payment</h3>

        {error && (
          <div className="bg-accent-coral/10 border border-accent-coral/30 text-accent-coral rounded-lg px-4 py-3 mb-4 text-sm">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm text-text-secondary mb-1">From Account</label>
            <select value={accountId} onChange={(e) => setAccountId(e.target.value)} className={inputClasses}>
              {accounts.map((a) => (
                <option key={a.id} value={a.id}>
                  {a.name} ({accountTypeLabel[a.accountType]}) — {formatCurrency(a.balance)}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm text-text-secondary mb-1">Payee Name</label>
            <input
              type="text"
              value={payeeName}
              onChange={(e) => setPayeeName(e.target.value)}
              placeholder="e.g. Netflix, Electricity Company"
              required
              className={inputClasses}
            />
          </div>

          <div>
            <label className="block text-sm text-text-secondary mb-1">Amount (AUD)</label>
            <input
              type="number"
              step="0.01"
              min="0.01"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              placeholder="0.00"
              required
              className={inputClasses}
            />
          </div>

          <div>
            <label className="block text-sm text-text-secondary mb-1">Frequency</label>
            <select value={frequency} onChange={(e) => setFrequency(e.target.value)} className={inputClasses}>
              <option value="OneOff">One-off</option>
              <option value="Weekly">Weekly</option>
              <option value="Fortnightly">Fortnightly</option>
              <option value="Monthly">Monthly</option>
              <option value="Quarterly">Quarterly</option>
              <option value="Yearly">Yearly</option>
            </select>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-sm text-text-secondary mb-1">Start Date</label>
              <input
                type="date"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                required
                className={inputClasses}
              />
            </div>
            <div>
              <label className="block text-sm text-text-secondary mb-1">
                End Date <span className="text-xs text-text-muted">(optional)</span>
              </label>
              <input
                type="date"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                className={inputClasses}
              />
            </div>
          </div>

          <div>
            <label className="block text-sm text-text-secondary mb-1">
              Description <span className="text-xs text-text-muted">(optional)</span>
            </label>
            <input
              type="text"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="e.g. Monthly subscription"
              className={inputClasses}
            />
          </div>

          <div className="flex gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 bg-dark-elevated text-text-primary font-medium rounded-lg py-3 border border-border hover:bg-dark-elevated/80 transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={submitting}
              className="flex-1 bg-gradient-to-r from-accent-teal to-accent-cyan hover:opacity-90 disabled:opacity-50 text-white font-semibold rounded-lg py-3 transition-opacity"
            >
              {submitting ? 'Creating...' : 'Create'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
