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
        <div className="w-8 h-8 border-2 border-accent-teal border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (!account) {
    return (
      <div className="text-center py-16">
        <p className="text-accent-coral mb-4">{error || 'Account not found'}</p>
        <Link to="/dashboard" className="text-accent-teal hover:underline text-sm font-medium">
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
        className="inline-flex items-center text-sm text-accent-teal hover:text-accent-teal/70 mb-4 transition-colors font-medium"
      >
        ← Back
      </Link>

      {/* Account card */}
      <div className="bg-dark-elevated rounded-xl p-5 mb-6 border border-border">
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-lg font-bold text-text-primary">{account.name}</h2>
          <span
            className={`text-xs font-medium px-2 py-0.5 rounded-full ${accountTypeBg[account.accountType]}`}
          >
            {accountTypeLabel[account.accountType]}
          </span>
        </div>
        <p className="text-xs text-text-secondary mb-3">
          BSB {account.bsb} · {account.accountNumber}
        </p>
        <div
          className={`text-3xl font-extrabold tracking-tight ${isNegative ? 'text-accent-coral' : 'text-accent-teal'}`}
        >
          {formatCurrency(account.balance)}
        </div>
        {account.availableBalance !== undefined && account.availableBalance !== account.balance && (
          <p className="text-sm text-text-secondary mt-1">
            Available: {formatCurrency(account.availableBalance)}
          </p>
        )}
      </div>

      {error && (
        <div className="bg-accent-coral/10 border border-accent-coral/30 text-accent-coral rounded-lg px-4 py-3 mb-4 text-sm">
          {error}
        </div>
      )}

      {/* Transactions */}
      <h3 className="text-sm font-semibold text-text-secondary uppercase tracking-wide mb-3">
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
