import { useCallback, useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import type { Account, Transaction } from '../types';
import {
  accountTypeLabel,
  accountTypeBg,
  formatCurrency,
} from '../types';
import { getAccount, getTransactions } from '../api/bankApi';
import TransactionList from '../components/TransactionList';

const PAGE_SIZE = 20;

export default function AccountDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [account, setAccount] = useState<Account | null>(null);
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);
  const [loadingAccount, setLoadingAccount] = useState(true);
  const [loadingTx, setLoadingTx] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!id) return;
    setLoadingAccount(true);
    getAccount(id)
      .then(setAccount)
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoadingAccount(false));
  }, [id]);

  useEffect(() => {
    if (!id) return;
    setLoadingTx(true);
    getTransactions(id, 1, PAGE_SIZE)
      .then((txs) => {
        setTransactions(txs);
        setHasMore(txs.length >= PAGE_SIZE);
        setPage(1);
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoadingTx(false));
  }, [id]);

  const loadMore = useCallback(() => {
    if (!id) return;
    const nextPage = page + 1;
    setLoadingTx(true);
    getTransactions(id, nextPage, PAGE_SIZE)
      .then((txs) => {
        setTransactions((prev) => [...prev, ...txs]);
        setHasMore(txs.length >= PAGE_SIZE);
        setPage(nextPage);
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoadingTx(false));
  }, [id, page]);

  if (loadingAccount) {
    return (
      <div className="flex justify-center py-16">
        <div className="w-8 h-8 border-2 border-brand-purple border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (!account) {
    return (
      <div className="text-center py-16">
        <p className="text-brand-red mb-4">{error || 'Account not found'}</p>
        <Link to="/dashboard" className="text-brand-blue hover:underline text-sm">
          Back to Dashboard
        </Link>
      </div>
    );
  }

  const isNegative = account.balance < 0;

  return (
    <div>
      {/* Back link */}
      <Link
        to="/dashboard"
        className="inline-flex items-center text-sm text-brand-blue hover:text-white mb-4 transition-colors"
      >
        ← Back
      </Link>

      {/* Account card */}
      <div className="bg-dark-card rounded-xl p-5 mb-6">
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-lg font-bold text-white">{account.name}</h2>
          <span
            className={`text-xs font-medium px-2 py-0.5 rounded-full ${accountTypeBg[account.accountType]}`}
          >
            {accountTypeLabel[account.accountType]}
          </span>
        </div>
        <p className="text-xs text-gray-400 mb-3">
          BSB {account.bsb} · {account.accountNumber}
        </p>
        <div
          className={`text-3xl font-bold ${isNegative ? 'text-brand-red' : 'text-brand-mint'}`}
        >
          {formatCurrency(account.balance)}
        </div>
      </div>

      {error && (
        <div className="bg-brand-red/10 border border-brand-red/30 text-brand-red rounded-lg px-4 py-3 mb-4 text-sm">
          {error}
        </div>
      )}

      {/* Transactions */}
      <h3 className="text-sm font-semibold text-gray-400 uppercase tracking-wide mb-3">
        Transactions
      </h3>
      <TransactionList
        transactions={transactions}
        loading={loadingTx}
        hasMore={hasMore}
        onLoadMore={loadMore}
      />
    </div>
  );
}
