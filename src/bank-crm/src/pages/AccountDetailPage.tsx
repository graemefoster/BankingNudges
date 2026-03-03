import { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getAccount, getTransactions, adjustBalance, closeAccount, getScheduledPayments } from '../api/crmApi';
import type { Account, Transaction, ScheduledPayment } from '../types';
import {
  TransactionStatus,
  formatCurrency, formatDateTime, formatDate, accountTypeLabel,
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
  const [scheduledPayments, setScheduledPayments] = useState<ScheduledPayment[]>([]);

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

  const loadScheduledPayments = useCallback(async () => {
    try {
      const sp = await getScheduledPayments(accountId);
      setScheduledPayments(sp);
    } catch (err) {
      console.error('Failed to load scheduled payments', err);
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

  useEffect(() => { loadAccount(); loadScheduledPayments(); }, [loadAccount, loadScheduledPayments]);
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
    return <div className="py-4 text-text-secondary text-xs">Loading...</div>;
  }

  return (
    <div>
      {/* Header */}
      <div className="flex items-center gap-2 mb-2 text-xs">
        <button onClick={() => navigate(-1)} className="text-crm-dark underline hover:no-underline cursor-pointer">
          &laquo; Back
        </button>
        <span className="text-text-secondary">|</span>
        <span className="font-bold">{account.name}</span>
        <span className="text-text-secondary">({accountTypeLabel[account.accountType]})</span>
        {!account.isActive && <span className="text-red-600 font-bold">[CLOSED]</span>}
      </div>

      {/* Account summary */}
      <fieldset className="mb-3">
        <legend>Account Summary</legend>
        <table className="text-xs bg-white">
          <tbody>
            <tr>
              <td className="px-2 py-1 bg-gray-50 font-bold w-32">Ledger Balance</td>
              <td className={`px-2 py-1 font-mono font-bold ${account.balance < 0 ? 'text-red-700' : ''}`}>
                {formatCurrency(account.balance)}
              </td>
            </tr>
            {account.availableBalance !== undefined && account.availableBalance !== account.balance && (
              <tr>
                <td className="px-2 py-1 bg-gray-50 font-bold">Available</td>
                <td className={`px-2 py-1 font-mono ${(account.availableBalance ?? 0) < 0 ? 'text-red-700' : ''}`}>
                  {formatCurrency(account.availableBalance ?? 0)}
                </td>
              </tr>
            )}
            <tr>
              <td className="px-2 py-1 bg-gray-50 font-bold">BSB</td>
              <td className="px-2 py-1 font-mono">{account.bsb}</td>
            </tr>
            <tr>
              <td className="px-2 py-1 bg-gray-50 font-bold">Account No.</td>
              <td className="px-2 py-1 font-mono">{account.accountNumber}</td>
            </tr>
            <tr>
              <td className="px-2 py-1 bg-gray-50 font-bold">Status</td>
              <td className="px-2 py-1">
                {account.isActive
                  ? <span className="text-crm-secondary">Active</span>
                  : <span className="text-red-600 font-bold">CLOSED</span>}
              </td>
            </tr>
          </tbody>
        </table>
        {account.isActive && (
          <div className="mt-2 flex gap-2">
            <button
              onClick={() => setShowAdjust(true)}
              className="px-3 py-1 bg-crm-dark text-white text-xs font-bold border border-crm-dark hover:bg-crm-dark/90 cursor-pointer"
            >
              Adjust Balance
            </button>
            <button
              onClick={() => setShowClose(true)}
              className="px-3 py-1 bg-white text-red-700 text-xs font-bold border border-red-400 hover:bg-red-50 cursor-pointer"
            >
              Close Account
            </button>
          </div>
        )}
      </fieldset>

      {/* Adjust dialog */}
      {showAdjust && (
        <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50" onClick={() => setShowAdjust(false)}>
          <div className="bg-crm-bg border border-border p-3 w-full max-w-sm" onClick={(e) => e.stopPropagation()}>
            <fieldset>
              <legend>Adjust Balance</legend>
              <table className="text-xs w-full">
                <tbody>
                  <tr>
                    <td className="px-2 py-1 bg-gray-50 font-bold w-24">Amount</td>
                    <td className="px-2 py-1">
                      <input type="number" step="0.01" value={adjustAmount}
                        onChange={(e) => setAdjustAmount(e.target.value)}
                        className="w-full px-1 py-0.5 border border-border text-xs font-mono"
                        placeholder="+100.00 or -50.00" />
                    </td>
                  </tr>
                  <tr>
                    <td className="px-2 py-1 bg-gray-50 font-bold">Reason</td>
                    <td className="px-2 py-1">
                      <input value={adjustReason}
                        onChange={(e) => setAdjustReason(e.target.value)}
                        className="w-full px-1 py-0.5 border border-border text-xs"
                        placeholder="Fee reversal, Interest correction..." />
                    </td>
                  </tr>
                </tbody>
              </table>
              <div className="flex justify-end gap-2 mt-2">
                <button onClick={() => setShowAdjust(false)} className="px-2 py-1 text-xs border border-border bg-white hover:bg-gray-50 cursor-pointer">
                  Cancel
                </button>
                <button
                  onClick={handleAdjust}
                  disabled={adjusting || !adjustAmount || !adjustReason.trim()}
                  className="px-3 py-1 bg-crm-dark text-white text-xs font-bold border border-crm-dark hover:bg-crm-dark/90 disabled:opacity-40 cursor-pointer"
                >
                  {adjusting ? 'Applying...' : 'Apply'}
                </button>
              </div>
            </fieldset>
          </div>
        </div>
      )}

      {/* Close dialog */}
      {showClose && (
        <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50" onClick={() => setShowClose(false)}>
          <div className="bg-crm-bg border border-border p-3 w-full max-w-sm" onClick={(e) => e.stopPropagation()}>
            <fieldset>
              <legend>Close Account</legend>
              <p className="text-xs mb-2">
                <strong>Warning:</strong> Closing this account is irreversible. Confirm below.
              </p>
              {closeError && (
                <div className="mb-2 p-1.5 bg-red-100 text-red-700 text-xs border border-red-300">
                  {closeError}
                </div>
              )}
              <div className="flex justify-end gap-2">
                <button onClick={() => setShowClose(false)} className="px-2 py-1 text-xs border border-border bg-white hover:bg-gray-50 cursor-pointer">
                  Cancel
                </button>
                {closeError.includes('non-zero') ? (
                  <button onClick={() => handleClose(true)} disabled={closing}
                    className="px-3 py-1 bg-red-700 text-white text-xs font-bold border border-red-700 hover:bg-red-800 disabled:opacity-40 cursor-pointer">
                    {closing ? 'Closing...' : 'Force Close'}
                  </button>
                ) : (
                  <button onClick={() => handleClose(false)} disabled={closing}
                    className="px-3 py-1 bg-red-700 text-white text-xs font-bold border border-red-700 hover:bg-red-800 disabled:opacity-40 cursor-pointer">
                    {closing ? 'Closing...' : 'Close Account'}
                  </button>
                )}
              </div>
            </fieldset>
          </div>
        </div>
      )}

      {/* Scheduled Payments */}
      {scheduledPayments.length > 0 && (
        <fieldset className="mb-3">
          <legend>Scheduled Payments</legend>
          <table className="w-full text-xs bg-white">
            <thead>
              <tr className="bg-crm-dark text-white text-left">
                <th className="px-2 py-1 font-normal">Payee</th>
                <th className="px-2 py-1 font-normal text-right">Amount</th>
                <th className="px-2 py-1 font-normal">Frequency</th>
                <th className="px-2 py-1 font-normal">Next Due</th>
                <th className="px-2 py-1 font-normal">Start</th>
                <th className="px-2 py-1 font-normal">Status</th>
              </tr>
            </thead>
            <tbody>
              {scheduledPayments.map((sp, i) => (
                <tr key={sp.id} className={`${i % 2 === 0 ? 'bg-white' : 'bg-gray-50'} ${!sp.isActive ? 'opacity-50' : ''}`}>
                  <td className="px-2 py-1 font-medium">{sp.payeeName}</td>
                  <td className="px-2 py-1 text-right font-mono text-red-700">
                    -{formatCurrency(sp.amount)}
                  </td>
                  <td className="px-2 py-1 text-text-secondary">{sp.frequency}</td>
                  <td className="px-2 py-1 text-text-secondary whitespace-nowrap">
                    {sp.isActive ? formatDate(sp.nextDueDate) : '—'}
                  </td>
                  <td className="px-2 py-1 text-text-secondary whitespace-nowrap">{formatDate(sp.startDate)}</td>
                  <td className="px-2 py-1">
                    {sp.isActive ? (
                      <span className="inline-block px-1.5 py-0.5 rounded bg-green-100 text-green-800 text-[10px] font-medium">Active</span>
                    ) : (
                      <span className="inline-block px-1.5 py-0.5 rounded bg-gray-100 text-gray-600 text-[10px] font-medium">Cancelled</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </fieldset>
      )}

      {/* Transactions */}
      <fieldset>
        <legend>Transaction History</legend>
        <div className="mb-2 flex items-center gap-2 text-xs">
          <label className="text-text-secondary">Filter by type:</label>
          <select value={typeFilter} onChange={(e) => { setTypeFilter(e.target.value); setPage(1); }}
            className="px-1 py-0.5 border border-border text-xs bg-white">
            <option value="">All</option>
            <option value="Deposit">Deposit</option>
            <option value="Withdrawal">Withdrawal</option>
            <option value="Transfer">Transfer</option>
            <option value="Interest">Interest</option>
            <option value="Repayment">Repayment</option>
            <option value="Adjustment">Adjustment</option>
            <option value="DirectDebit">Direct Debit</option>
          </select>
          <span className="text-text-secondary ml-auto">{total} record(s)</span>
        </div>

        <table className="w-full text-xs bg-white">
          <thead>
            <tr className="bg-crm-dark text-white text-left">
              <th className="px-2 py-1 font-normal">Date</th>
              <th className="px-2 py-1 font-normal">Description</th>
              <th className="px-2 py-1 font-normal">Type</th>
              <th className="px-2 py-1 font-normal text-right">Amount</th>
              <th className="px-2 py-1 font-normal">Status</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td colSpan={5} className="px-2 py-4 text-center text-text-secondary">Loading...</td></tr>
            ) : transactions.length === 0 ? (
              <tr><td colSpan={5} className="px-2 py-4 text-center text-text-secondary">No transactions found.</td></tr>
            ) : (
              transactions.map((t, i) => (
                <tr key={t.id} className={`${i % 2 === 0 ? 'bg-white' : 'bg-gray-50'} ${t.status === TransactionStatus.Pending ? 'opacity-70' : ''}`}>
                  <td className="px-2 py-1 text-text-secondary whitespace-nowrap">{formatDateTime(t.createdAt)}</td>
                  <td className="px-2 py-1">{t.description}</td>
                  <td className="px-2 py-1 text-text-secondary">{transactionTypeLabel[t.transactionType]}</td>
                  <td className={`px-2 py-1 text-right font-mono ${t.amount >= 0 ? 'text-crm-secondary' : 'text-red-700'}`}>
                    {t.amount >= 0 ? '+' : ''}{formatCurrency(t.amount)}
                  </td>
                  <td className="px-2 py-1">
                    {t.status === TransactionStatus.Pending ? (
                      <span className="inline-block px-1.5 py-0.5 rounded bg-amber-100 text-amber-800 text-[10px] font-medium">Pending</span>
                    ) : (
                      <span className="inline-block px-1.5 py-0.5 rounded bg-green-100 text-green-800 text-[10px] font-medium">Settled</span>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="flex items-center gap-2 mt-2 text-xs">
            <button onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page === 1}
              className="px-2 py-0.5 bg-white border border-border hover:bg-gray-100 disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer">
              &laquo; Prev
            </button>
            <span className="text-text-secondary">Page {page} of {totalPages}</span>
            <button onClick={() => setPage((p) => Math.min(totalPages, p + 1))} disabled={page === totalPages}
              className="px-2 py-0.5 bg-white border border-border hover:bg-gray-100 disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer">
              Next &raquo;
            </button>
          </div>
        )}
      </fieldset>
    </div>
  );
}
