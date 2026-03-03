import type { Transaction } from '../types';
import {
  transactionTypeLabel,
  formatCurrency,
} from '../types';

interface Props {
  transactions: Transaction[];
  accountBalance: number;
  loading: boolean;
  onLoadMore?: () => void;
  hasMore?: boolean;
}

function txColor(t: Transaction): string {
  return t.amount >= 0 ? 'text-accent-teal' : 'text-accent-coral';
}

function txSign(t: Transaction): string {
  return t.amount >= 0 ? '+' : '';
}

function dateKey(iso: string): string {
  return new Date(iso).toLocaleDateString('en-AU');
}

function isToday(iso: string): boolean {
  const d = new Date(iso);
  const now = new Date();
  return (
    d.getFullYear() === now.getFullYear() &&
    d.getMonth() === now.getMonth() &&
    d.getDate() === now.getDate()
  );
}

function formatDayLabel(iso: string): string {
  if (isToday(iso)) return 'Today';
  const d = new Date(iso);
  const yesterday = new Date();
  yesterday.setDate(yesterday.getDate() - 1);
  if (
    d.getFullYear() === yesterday.getFullYear() &&
    d.getMonth() === yesterday.getMonth() &&
    d.getDate() === yesterday.getDate()
  )
    return 'Yesterday';
  return d.toLocaleDateString('en-AU', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
}

interface DayGroup {
  label: string;
  closingBalance: number;
  transactions: Transaction[];
}

function groupByDay(
  transactions: Transaction[],
  accountBalance: number,
): DayGroup[] {
  if (transactions.length === 0) return [];

  const groups: DayGroup[] = [];
  let currentKey = '';
  let currentTxns: Transaction[] = [];

  for (const tx of transactions) {
    const key = dateKey(tx.createdAt);
    if (key !== currentKey) {
      if (currentTxns.length > 0) {
        groups.push({ label: '', closingBalance: 0, transactions: currentTxns });
      }
      currentKey = key;
      currentTxns = [tx];
    } else {
      currentTxns.push(tx);
    }
  }
  if (currentTxns.length > 0) {
    groups.push({ label: '', closingBalance: 0, transactions: currentTxns });
  }

  // Compute closing balance per day.
  // Transactions are newest-first, so groups[0] is the most recent day.
  // Closing balance for newest day = account.balance.
  // For each older day: subtract settled amounts of the newer day.
  let balance = accountBalance;
  for (const group of groups) {
    group.label = formatDayLabel(group.transactions[0].createdAt);
    group.closingBalance = balance;
    const settledSum = group.transactions
      .filter((t) => t.status === 'Settled')
      .reduce((sum, t) => sum + t.amount, 0);
    balance -= settledSum;
  }

  return groups;
}

export default function TransactionList({
  transactions,
  accountBalance,
  loading,
  onLoadMore,
  hasMore,
}: Props) {
  if (!loading && transactions.length === 0) {
    return <p className="text-text-secondary text-center py-8">No transactions yet</p>;
  }

  const groups = groupByDay(transactions, accountBalance);

  return (
    <div>
      {groups.map((group, gi) => (
        <div key={gi}>
          {/* Day divider */}
          <div className="flex items-center justify-between py-2 mt-2 first:mt-0">
            <span className="text-xs font-semibold text-text-secondary uppercase tracking-wide">
              {group.label}
            </span>
            <span className="text-xs font-medium text-text-secondary">
              {formatCurrency(group.closingBalance)}
            </span>
          </div>

          {/* Transactions for this day */}
          <div className="space-y-1.5">
            {group.transactions.map((tx) => (
              <div
                key={tx.id}
                className={`bg-dark-elevated rounded-lg px-4 py-3 flex items-center justify-between border border-border ${tx.status === 'Pending' ? 'opacity-70 border-accent-amber/30' : ''}`}
              >
                <div className="flex-1 min-w-0 mr-3">
                  <p className="text-sm font-medium text-text-primary truncate">
                    {tx.description}
                  </p>
                  <p className="text-xs text-text-secondary mt-0.5">
                    {transactionTypeLabel[tx.transactionType]} ·{' '}
                    {new Date(tx.createdAt).toLocaleTimeString('en-AU', {
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
                  <p className="text-xs text-text-muted">
                    {tx.status === 'Pending' ? (
                      <span className="text-accent-amber">Pending</span>
                    ) : (
                      transactionTypeLabel[tx.transactionType]
                    )}
                  </p>
                </div>
              </div>
            ))}
          </div>
        </div>
      ))}

      {loading && (
        <div className="flex justify-center py-4">
          <div className="w-6 h-6 border-2 border-accent-teal border-t-transparent rounded-full animate-spin" />
        </div>
      )}

      {!loading && hasMore && onLoadMore && (
        <button
          onClick={onLoadMore}
          className="w-full py-2 text-sm text-accent-teal hover:text-accent-teal/70 transition-colors font-medium"
        >
          Load more
        </button>
      )}
    </div>
  );
}
