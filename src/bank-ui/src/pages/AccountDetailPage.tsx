import { useCallback, useEffect, useMemo, useState } from 'react';
import { useParams, useSearchParams, Link, useNavigate } from 'react-router-dom';
import type { Account, Transaction, TransactionFilters } from '../types';
import {
  accountTypeLabel,
  accountTypeBg,
  formatCurrency,
  merchantCategories,
} from '../types';
import { getAccount, getTransactions } from '../api/bankApi';
import TransactionList from '../components/TransactionList';

const PAGE_SIZE = 20;

export default function AccountDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const [account, setAccount] = useState<Account | null>(null);
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);
  const [loadingAccount, setLoadingAccount] = useState(true);
  const [loadingTx, setLoadingTx] = useState(true);
  const [error, setError] = useState('');

  // Filter state — initialise from URL query params for deep-linking
  const [searchText, setSearchText] = useState(searchParams.get('search') ?? '');
  const [activeSearch, setActiveSearch] = useState(searchParams.get('search') ?? '');
  const [category, setCategory] = useState(searchParams.get('category') ?? '');

  // Debounce search text → activeSearch (300 ms)
  useEffect(() => {
    const timer = setTimeout(() => setActiveSearch(searchText), 300);
    return () => clearTimeout(timer);
  }, [searchText]);

  // Keep URL params in sync with active filters
  useEffect(() => {
    const params: Record<string, string> = {};
    if (activeSearch) params.search = activeSearch;
    if (category) params.category = category;
    setSearchParams(params, { replace: true });
  }, [activeSearch, category, setSearchParams]);

  const filters = useMemo<TransactionFilters | undefined>(() => {
    const f: TransactionFilters = {};
    if (activeSearch) f.search = activeSearch;
    if (category) f.category = category;
    return f.search || f.category ? f : undefined;
  }, [activeSearch, category]);

  const hasActiveFilters = !!activeSearch || !!category;

  const clearFilters = useCallback(() => {
    setSearchText('');
    setActiveSearch('');
    setCategory('');
  }, []);

  useEffect(() => {
    if (!id) return;
    setLoadingAccount(true);
    getAccount(id)
      .then(setAccount)
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoadingAccount(false));
  }, [id]);

  // Fetch transactions when account or filters change — reset to page 1
  useEffect(() => {
    if (!id) return;
    setLoadingTx(true);
    setTransactions([]);
    getTransactions(id, 1, PAGE_SIZE, filters)
      .then((txs) => {
        setTransactions(txs);
        setHasMore(txs.length >= PAGE_SIZE);
        setPage(1);
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoadingTx(false));
  }, [id, activeSearch, category]);

  const loadMore = useCallback(() => {
    if (!id) return;
    const nextPage = page + 1;
    setLoadingTx(true);
    getTransactions(id, nextPage, PAGE_SIZE, filters)
      .then((txs) => {
        setTransactions((prev) => [...prev, ...txs]);
        setHasMore(txs.length >= PAGE_SIZE);
        setPage(nextPage);
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoadingTx(false));
  }, [id, page, filters]);

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
      <button
        onClick={() => navigate(-1)}
        className="inline-flex items-center text-sm text-accent-teal hover:text-accent-teal/70 mb-4 transition-colors font-medium"
      >
        ← Back
      </button>

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

      {/* Transaction filters */}
      <div className="flex gap-2 mb-4">
        <div className="relative flex-1">
          <svg
            className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-text-muted"
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            strokeWidth={2}
            stroke="currentColor"
          >
            <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-4.35-4.35M11 19a8 8 0 100-16 8 8 0 000 16z" />
          </svg>
          <input
            type="text"
            placeholder="Search transactions..."
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            className="w-full bg-dark-surface border border-border rounded-lg pl-9 pr-3 py-2 text-sm text-text-primary placeholder:text-text-muted focus:outline-none focus:border-border-focus"
          />
        </div>
        <select
          value={category}
          onChange={(e) => setCategory(e.target.value)}
          className="bg-dark-surface border border-border rounded-lg px-3 py-2 text-sm text-text-primary focus:outline-none focus:border-border-focus appearance-none"
        >
          <option value="">All categories</option>
          {merchantCategories.map((cat) => (
            <option key={cat} value={cat}>
              {cat}
            </option>
          ))}
        </select>
      </div>

      {hasActiveFilters && (
        <div className="flex items-center justify-between mb-3 px-1">
          <span className="text-xs text-accent-teal font-medium">Filtered results</span>
          <button
            onClick={clearFilters}
            className="text-xs text-text-secondary hover:text-text-primary"
          >
            ✕ Clear filters
          </button>
        </div>
      )}

      {/* Transactions */}
      <h3 className="text-sm font-semibold text-text-secondary uppercase tracking-wide mb-3">
        Transactions
      </h3>
      <TransactionList
        transactions={transactions}
        accountBalance={account.balance}
        loading={loadingTx}
        hasMore={hasMore}
        onLoadMore={loadMore}
      />
    </div>
  );
}
