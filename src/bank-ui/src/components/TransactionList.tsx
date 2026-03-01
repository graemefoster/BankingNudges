import type { Transaction } from '../types';
import {
  transactionTypeLabel,
  formatCurrency,
} from '../types';

interface Props {
  transactions: Transaction[];
  loading: boolean;
  onLoadMore?: () => void;
  hasMore?: boolean;
}

function txColor(t: Transaction): string {
  return t.amount >= 0 ? 'text-brand-mint' : 'text-brand-red';
}

function txSign(t: Transaction): string {
  return t.amount >= 0 ? '+' : '';
}

export default function TransactionList({
  transactions,
  loading,
  onLoadMore,
  hasMore,
}: Props) {
  if (!loading && transactions.length === 0) {
    return <p className="text-gray-400 text-center py-8">No transactions yet</p>;
  }

  return (
    <div className="space-y-2">
      {transactions.map((tx) => (
        <div
          key={tx.id}
          className="bg-dark-card rounded-lg px-4 py-3 flex items-center justify-between"
        >
          <div className="flex-1 min-w-0 mr-3">
            <p className="text-sm font-medium text-white truncate">
              {tx.description}
            </p>
            <p className="text-xs text-gray-400 mt-0.5">
              {transactionTypeLabel[tx.transactionType]} ·{' '}
              {new Date(tx.createdAt).toLocaleDateString('en-AU', {
                day: 'numeric',
                month: 'short',
                year: 'numeric',
                hour: '2-digit',
                minute: '2-digit',
              })}
            </p>
          </div>
          <div className="text-right shrink-0">
            <p className={`text-sm font-semibold ${txColor(tx)}`}>
              {txSign(tx)}
              {formatCurrency(Math.abs(tx.amount))}
            </p>
            <p className="text-xs text-gray-500">
              Bal: {formatCurrency(tx.balanceAfter)}
            </p>
          </div>
        </div>
      ))}

      {loading && (
        <div className="flex justify-center py-4">
          <div className="w-6 h-6 border-2 border-brand-purple border-t-transparent rounded-full animate-spin" />
        </div>
      )}

      {!loading && hasMore && onLoadMore && (
        <button
          onClick={onLoadMore}
          className="w-full py-2 text-sm text-brand-blue hover:text-white transition-colors"
        >
          Load more
        </button>
      )}
    </div>
  );
}
