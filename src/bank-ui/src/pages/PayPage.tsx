import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import type { Account, PayeeLookup } from '../types';
import {
  AccountType,
  accountTypeLabel,
  formatCurrency,
} from '../types';
import { getCustomerAccounts, lookupAccount, pay } from '../api/bankApi';

export default function PayPage() {
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [fromId, setFromId] = useState('');
  const [bsb, setBsb] = useState('');
  const [accountNumber, setAccountNumber] = useState('');
  const [payee, setPayee] = useState<PayeeLookup | null>(null);
  const [amount, setAmount] = useState('');
  const [description, setDescription] = useState('');
  const [reference, setReference] = useState('');
  const [loading, setLoading] = useState(true);
  const [lookingUp, setLookingUp] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const navigate = useNavigate();

  const customerId = sessionStorage.getItem('customerId');

  useEffect(() => {
    if (!customerId) {
      navigate('/');
      return;
    }
    getCustomerAccounts(customerId)
      .then((accs) => {
        // Filter to accounts that can send payments (no Home Loans)
        const sendable = accs.filter(
          (a) => a.accountType !== AccountType.HomeLoan,
        );
        setAccounts(sendable);
        if (sendable.length > 0) {
          setFromId(sendable[0].id);
        }
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, [customerId, navigate]);

  async function handleLookup() {
    setError('');
    setPayee(null);
    const cleanBsb = bsb.trim();
    const cleanAccNo = accountNumber.trim();
    if (!cleanBsb || !cleanAccNo) {
      setError('Enter both BSB and account number');
      return;
    }
    setLookingUp(true);
    try {
      const result = await lookupAccount(cleanBsb, cleanAccNo);
      setPayee(result);
    } catch (err) {
      setError(
        err instanceof Error ? err.message : 'Account not found',
      );
    } finally {
      setLookingUp(false);
    }
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    setSuccess('');

    if (!payee) {
      setError('Look up the recipient first');
      return;
    }

    const amt = parseFloat(amount);
    if (isNaN(amt) || amt <= 0) {
      setError('Enter a valid amount');
      return;
    }
    if (!fromId) {
      setError('Select a source account');
      return;
    }

    setSubmitting(true);
    try {
      await pay(
        customerId!,
        fromId,
        bsb.trim(),
        accountNumber.trim(),
        amt,
        description || `Payment to ${payee.customerName}`,
        reference || `Payment from ${sessionStorage.getItem('customerName') ?? 'Customer'}`,
      );
      setSuccess(`Paid ${formatCurrency(amt)} to ${payee.customerName}`);
      setAmount('');
      setDescription('');
      setReference('');
      setPayee(null);
      setBsb('');
      setAccountNumber('');
      setTimeout(() => navigate('/dashboard'), 1500);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Payment failed');
    } finally {
      setSubmitting(false);
    }
  }

  if (loading) {
    return (
      <div className="flex justify-center py-16">
        <div className="w-8 h-8 border-2 border-accent-teal border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (accounts.length === 0) {
    return (
      <div className="text-center py-16">
        <p className="text-text-secondary">No accounts available to pay from.</p>
      </div>
    );
  }

  const renderOption = (a: Account) =>
    `${a.name} (${accountTypeLabel[a.accountType]}) — ${formatCurrency(a.balance)}`;

  const inputClasses =
    'w-full bg-dark-elevated text-text-primary rounded-lg px-4 py-3 border border-border focus:border-accent-teal focus:ring-2 focus:ring-border-focus focus:outline-none';

  return (
    <div>
      <h2 className="text-xl font-bold text-text-primary mb-6">Pay Someone</h2>

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

      <form onSubmit={handleSubmit} className="space-y-4">
        {/* From */}
        <div>
          <label className="block text-sm text-text-secondary mb-1">From</label>
          <select
            value={fromId}
            onChange={(e) => setFromId(e.target.value)}
            className={inputClasses}
          >
            {accounts.map((a) => (
              <option key={a.id} value={a.id}>
                {renderOption(a)}
              </option>
            ))}
          </select>
        </div>

        {/* Recipient lookup */}
        <div className="bg-dark-surface rounded-xl p-4 border border-border space-y-3">
          <p className="text-sm font-semibold text-text-primary">Recipient</p>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs text-text-secondary mb-1">BSB</label>
              <input
                type="text"
                value={bsb}
                onChange={(e) => { setBsb(e.target.value); setPayee(null); }}
                placeholder="062-000"
                className={inputClasses}
              />
            </div>
            <div>
              <label className="block text-xs text-text-secondary mb-1">Account Number</label>
              <input
                type="text"
                value={accountNumber}
                onChange={(e) => { setAccountNumber(e.target.value); setPayee(null); }}
                placeholder="12345678"
                className={inputClasses}
              />
            </div>
          </div>
          <button
            type="button"
            onClick={handleLookup}
            disabled={lookingUp}
            className="w-full bg-dark-elevated hover:bg-dark-elevated/80 disabled:opacity-50 text-text-primary font-medium rounded-lg py-2 text-sm transition-colors border border-border"
          >
            {lookingUp ? 'Looking up...' : 'Look Up Recipient'}
          </button>

          {payee && (
            <div className="bg-accent-teal/10 border border-accent-teal/30 rounded-lg px-4 py-3">
              <p className="text-sm font-semibold text-accent-teal">{payee.customerName}</p>
              <p className="text-xs text-accent-teal/70">
                {payee.name} · {payee.accountTypeName}
              </p>
            </div>
          )}
        </div>

        {/* Amount */}
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

        {/* Description (your statement) */}
        <div>
          <label className="block text-sm text-text-secondary mb-1">
            Description <span className="text-xs text-text-muted">(appears on your statement)</span>
          </label>
          <input
            type="text"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="e.g. Rent March 2026"
            className={inputClasses}
          />
        </div>

        {/* Reference (recipient's statement) */}
        <div>
          <label className="block text-sm text-text-secondary mb-1">
            Reference <span className="text-xs text-text-muted">(appears on recipient's statement)</span>
          </label>
          <input
            type="text"
            value={reference}
            onChange={(e) => setReference(e.target.value)}
            placeholder="e.g. Invoice #1234"
            className={inputClasses}
          />
        </div>

        <button
          type="submit"
          disabled={submitting || !payee}
          className="w-full bg-gradient-to-r from-accent-teal to-accent-cyan hover:opacity-90 disabled:opacity-50 text-white font-semibold rounded-lg py-3 transition-opacity"
        >
          {submitting ? 'Paying...' : 'Pay'}
        </button>
      </form>
    </div>
  );
}
