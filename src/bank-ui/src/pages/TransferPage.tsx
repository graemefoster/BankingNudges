import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import type { Account } from '../types';
import {
  accountTypeLabel,
  formatCurrency,
} from '../types';
import { getCustomerAccounts, transfer } from '../api/bankApi';

export default function TransferPage() {
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [fromId, setFromId] = useState('');
  const [toId, setToId] = useState('');
  const [amount, setAmount] = useState('');
  const [description, setDescription] = useState('');
  const [loading, setLoading] = useState(true);
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
        setAccounts(accs);
        if (accs.length >= 2) {
          setFromId(accs[0].id);
          setToId(accs[1].id);
        } else if (accs.length === 1) {
          setFromId(accs[0].id);
        }
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, [customerId, navigate]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    setSuccess('');

    const amt = parseFloat(amount);
    if (isNaN(amt) || amt <= 0) {
      setError('Enter a valid amount');
      return;
    }
    if (fromId === toId) {
      setError('From and To accounts must be different');
      return;
    }
    if (!fromId || !toId) {
      setError('Select both accounts');
      return;
    }

    setSubmitting(true);
    try {
      await transfer(fromId, toId, amt, description || 'Transfer');
      setSuccess(`Transferred ${formatCurrency(amt)} successfully`);
      setAmount('');
      setDescription('');
      // Navigate to dashboard after brief delay
      setTimeout(() => navigate('/dashboard'), 1500);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Transfer failed');
    } finally {
      setSubmitting(false);
    }
  }

  if (loading) {
    return (
      <div className="flex justify-center py-16">
        <div className="w-8 h-8 border-2 border-brand-purple border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (accounts.length < 2) {
    return (
      <div className="text-center py-16">
        <p className="text-text-secondary">You need at least 2 accounts to transfer.</p>
      </div>
    );
  }

  const renderOption = (a: Account) =>
    `${a.name} (${accountTypeLabel[a.accountType]}) — ${formatCurrency(a.balance)}`;

  return (
    <div>
      <h2 className="text-xl font-bold text-text-primary mb-6">Transfer Funds</h2>

      {error && (
        <div className="bg-brand-red/10 border border-brand-red/30 text-brand-red rounded-lg px-4 py-3 mb-4 text-sm">
          {error}
        </div>
      )}
      {success && (
        <div className="bg-emerald-50 border border-emerald-200 text-emerald-700 rounded-lg px-4 py-3 mb-4 text-sm">
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
            className="w-full bg-light-card text-text-primary rounded-lg px-4 py-3 border border-gray-200 focus:border-brand-purple focus:ring-2 focus:ring-brand-purple/20 focus:outline-none shadow-sm"
          >
            {accounts.map((a) => (
              <option key={a.id} value={a.id}>
                {renderOption(a)}
              </option>
            ))}
          </select>
        </div>

        {/* To */}
        <div>
          <label className="block text-sm text-text-secondary mb-1">To</label>
          <select
            value={toId}
            onChange={(e) => setToId(e.target.value)}
            className="w-full bg-light-card text-text-primary rounded-lg px-4 py-3 border border-gray-200 focus:border-brand-purple focus:ring-2 focus:ring-brand-purple/20 focus:outline-none shadow-sm"
          >
            {accounts.map((a) => (
              <option key={a.id} value={a.id}>
                {renderOption(a)}
              </option>
            ))}
          </select>
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
            className="w-full bg-light-card text-text-primary rounded-lg px-4 py-3 border border-gray-200 focus:border-brand-purple focus:ring-2 focus:ring-brand-purple/20 focus:outline-none shadow-sm"
          />
        </div>

        {/* Description */}
        <div>
          <label className="block text-sm text-text-secondary mb-1">
            Description (optional)
          </label>
          <input
            type="text"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="What's this for?"
            className="w-full bg-light-card text-text-primary rounded-lg px-4 py-3 border border-gray-200 focus:border-brand-purple focus:ring-2 focus:ring-brand-purple/20 focus:outline-none shadow-sm"
          />
        </div>

        <button
          type="submit"
          disabled={submitting}
          className="w-full bg-brand-purple hover:bg-brand-purple/80 disabled:opacity-50 text-white font-semibold rounded-lg py-3 transition-colors shadow-md"
        >
          {submitting ? 'Transferring...' : 'Transfer'}
        </button>
      </form>
    </div>
  );
}
