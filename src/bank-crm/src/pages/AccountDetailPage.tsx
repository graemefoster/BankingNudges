import { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getAccount, getTransactions, adjustBalance, closeAccount } from '../api/crmApi';
import type { Account, Transaction } from '../types';
import {
  formatCurrency, formatDateTime, accountTypeLabel, accountTypeBadge,
  transactionTypeLabel,
} from '../types';

export default function AccountDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const accountId = Number(id);

  const [account, setAccount] = useState<Account | null>(null);
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [typeFilter, setTypeFilter] = useState('');
  const [loading, setLoading] = useState(true);

  // Adjust modal
  const [showAdjust, setShowAdjust] = useState(false);
  const [adjustAmount, setAdjustAmount] = useState('');
  const [adjustReason, setAdjustReason] = useState('');
  const [adjusting, setAdjusting] = useState(false);

  // Close modal
  const [showClose, setShowClose] = useState(false);
  const [closing, setClosing] = useState(false);
  const [closeError, setCloseError] = useState('');

  const loadAccount = useCallback(async () => {
    try {
      const a = await getAccount(accountId);
      setAccount(a);
    } catch (err) {
      console.error('Failed to load account', err);
    }
  }, [accountId]);

  const loadTransactions = useCallback(async () => {
    setLoading(true);
    try {
      const data = await getTransactions(accountId, {
        type: typeFilter || undefined,
        page,
      });
      setTransactions(data.transactions);
      setTotal(data.total);
    } catch (err) {
      console.error('Failed to load transactions', err);
    } finally {
      setLoading(false);
    }
  }, [accountId, typeFilter, page]);

  useEffect(() => { loadAccount(); }, [loadAccount]);
  useEffect(() => { loadTransactions(); }, [loadTransactions]);

  const handleAdjust = async () => {
    const amount = parseFloat(adjustAmount);
    if (isNaN(amount) || !adjustReason.trim()) return;
    setAdjusting(true);
    try {
      await adjustBalance(accountId, amount, adjustReason.trim());
      setShowAdjust(false);
      setAdjustAmount('');
      setAdjustReason('');
      await Promise.all([loadAccount(), loadTransactions()]);
    } catch (err) {
      console.error('Failed to adjust', err);
    } finally {
      setAdjusting(false);
    }
  };

  const handleClose = async (force: boolean) => {
    setClosing(true);
    setCloseError('');
    try {
      await closeAccount(accountId, force);
      setShowClose(false);
      await loadAccount();
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Failed to close account';
      if (msg.includes('non-zero balance')) {
        setCloseError('Account has a non-zero balance. Force close?');
      } else {
        setCloseError(msg);
      }
    } finally {
      setClosing(false);
    }
  };

  const totalPages = Math.ceil(total / 20);

  if (!account) {
    return <div className="text-center py-12 text-text-secondary">Loading...</div>;
  }

  return (
    <div>
      {/* Header */}
      <div className="flex items-center gap-3 mb-6">
        <button onClick={() => navigate(-1)} className="text-crm-secondary hover:text-crm-dark transition-colors">
          ← Back
        </button>
        <h1 className="text-2xl font-bold text-crm-dark">{account.name}</h1>
        <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${accountTypeBadge[account.accountType]}`}>
          {accountTypeLabel[account.accountType]}
        </span>
        {!account.isActive && (
          <span className="text-xs px-2 py-0.5 rounded-full bg-gray-200 text-gray-600 font-medium">Closed</span>
        )}
      </div>

      {/* Account summary card */}
      <div className="bg-crm-card rounded-xl shadow-sm p-6 mb-6">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-sm text-text-secondary">Current Balance</p>
            <p className={`text-3xl font-bold ${account.balance < 0 ? 'text-crm-warning' : 'text-crm-dark'}`}>
              {formatCurrency(account.balance)}
            </p>
            <p className="text-xs text-text-secondary mt-1">
              BSB: {account.bsb} | Account: {account.accountNumber}
            </p>
          </div>
          {account.isActive && (
            <div className="flex gap-2">
              <button
                onClick={() => setShowAdjust(true)}
                className="px-4 py-2 bg-crm-accent text-crm-dark text-sm font-medium rounded-lg hover:bg-crm-accent/90 transition-colors"
              >
                Adjust Balance
              </button>
              <button
                onClick={() => setShowClose(true)}
                className="px-4 py-2 bg-crm-warning/10 text-crm-warning text-sm font-medium rounded-lg hover:bg-crm-warning/20 transition-colors"
              >
                Close Account
              </button>
            </div>
          )}
        </div>
      </div>

      {/* Adjust modal */}
      {showAdjust && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50" onClick={() => setShowAdjust(false)}>
          <div className="bg-crm-card rounded-xl shadow-xl p-6 w-full max-w-md" onClick={(e) => e.stopPropagation()}>
            <h3 className="text-lg font-semibold text-crm-dark mb-4">Adjust Balance</h3>
            <div className="space-y-3">
              <div>
                <label className="block text-xs font-medium text-text-secondary mb-1">Amount (positive to credit, negative to debit)</label>
                <input
                  type="number" step="0.01" value={adjustAmount}
                  onChange={(e) => setAdjustAmount(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-crm-accent"
                  placeholder="e.g. 100.00 or -50.00"
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-text-secondary mb-1">Reason (required)</label>
                <input
                  value={adjustReason}
                  onChange={(e) => setAdjustReason(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-crm-accent"
                  placeholder="e.g. Fee reversal, Interest correction"
                />
              </div>
            </div>
            <div className="flex justify-end gap-2 mt-4">
              <button onClick={() => setShowAdjust(false)} className="px-4 py-2 text-sm text-text-secondary hover:text-text-primary">
                Cancel
              </button>
              <button
                onClick={handleAdjust}
                disabled={adjusting || !adjustAmount || !adjustReason.trim()}
                className="px-4 py-2 bg-crm-accent text-crm-dark text-sm font-medium rounded-lg hover:bg-crm-accent/90 disabled:opacity-40"
              >
                {adjusting ? 'Adjusting...' : 'Apply Adjustment'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Close modal */}
      {showClose && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50" onClick={() => setShowClose(false)}>
          <div className="bg-crm-card rounded-xl shadow-xl p-6 w-full max-w-md" onClick={(e) => e.stopPropagation()}>
            <h3 className="text-lg font-semibold text-crm-warning mb-2">Close Account</h3>
            <p className="text-sm text-text-secondary mb-4">
              Are you sure you want to close this account? This action cannot be undone.
            </p>
            {closeError && (
              <div className="mb-3 p-3 bg-crm-warning/10 text-crm-warning text-sm rounded-lg">
                {closeError}
              </div>
            )}
            <div className="flex justify-end gap-2">
              <button onClick={() => setShowClose(false)} className="px-4 py-2 text-sm text-text-secondary hover:text-text-primary">
                Cancel
              </button>
              {closeError.includes('non-zero') ? (
                <button
                  onClick={() => handleClose(true)}
                  disabled={closing}
                  className="px-4 py-2 bg-crm-warning text-white text-sm font-medium rounded-lg hover:bg-crm-warning/90 disabled:opacity-40"
                >
                  {closing ? 'Closing...' : 'Force Close'}
                </button>
              ) : (
                <button
                  onClick={() => handleClose(false)}
                  disabled={closing}
                  className="px-4 py-2 bg-crm-warning text-white text-sm font-medium rounded-lg hover:bg-crm-warning/90 disabled:opacity-40"
                >
                  {closing ? 'Closing...' : 'Close Account'}
                </button>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Transactions */}
      <div className="bg-crm-card rounded-xl shadow-sm overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-crm-dark">Transactions</h2>
          <select
            value={typeFilter}
            onChange={(e) => { setTypeFilter(e.target.value); setPage(1); }}
            className="text-sm border border-gray-200 rounded-lg px-3 py-1.5 focus:outline-none focus:ring-2 focus:ring-crm-accent"
          >
            <option value="">All types</option>
            <option value="Deposit">Deposit</option>
            <option value="Withdrawal">Withdrawal</option>
            <option value="Transfer">Transfer</option>
            <option value="Interest">Interest</option>
            <option value="Repayment">Repayment</option>
            <option value="Adjustment">Adjustment</option>
          </select>
        </div>

        <table className="w-full text-sm">
          <thead>
            <tr className="bg-crm-dark/5 text-left">
              <th className="px-6 py-3 font-medium text-text-secondary">Date</th>
              <th className="px-6 py-3 font-medium text-text-secondary">Description</th>
              <th className="px-6 py-3 font-medium text-text-secondary">Type</th>
              <th className="px-6 py-3 font-medium text-text-secondary text-right">Amount</th>
              <th className="px-6 py-3 font-medium text-text-secondary text-right">Balance</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td colSpan={5} className="px-6 py-8 text-center text-text-secondary">Loading...</td></tr>
            ) : transactions.length === 0 ? (
              <tr><td colSpan={5} className="px-6 py-8 text-center text-text-secondary">No transactions</td></tr>
            ) : (
              transactions.map((t) => (
                <tr key={t.id} className="border-t border-gray-50">
                  <td className="px-6 py-3 text-text-secondary">{formatDateTime(t.createdAt)}</td>
                  <td className="px-6 py-3">{t.description}</td>
                  <td className="px-6 py-3">
                    <span className="text-xs text-text-secondary">{transactionTypeLabel[t.transactionType]}</span>
                  </td>
                  <td className={`px-6 py-3 text-right font-medium ${t.amount >= 0 ? 'text-crm-secondary' : 'text-crm-warning'}`}>
                    {t.amount >= 0 ? '+' : ''}{formatCurrency(t.amount)}
                  </td>
                  <td className="px-6 py-3 text-right text-text-secondary">{formatCurrency(t.balanceAfter)}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="flex items-center justify-center gap-2 py-4 border-t border-gray-100">
            <button
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page === 1}
              className="px-3 py-1.5 text-sm bg-crm-bg border border-gray-200 rounded-lg hover:bg-crm-card-hover disabled:opacity-40"
            >
              Previous
            </button>
            <span className="text-sm text-text-secondary">Page {page} of {totalPages}</span>
            <button
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
              className="px-3 py-1.5 text-sm bg-crm-bg border border-gray-200 rounded-lg hover:bg-crm-card-hover disabled:opacity-40"
            >
              Next
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
