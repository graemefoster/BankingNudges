import { useState } from 'react';
import type { Transaction } from '../types';
import {
  TransactionStatus,
  transactionTypeLabel,
  formatCurrency,
  formatForeignCurrency,
} from '../types';

const categoryIcons: Record<string, string> = {
  Groceries: '🛒',
  Fuel: '⛽',
  Dining: '🍔',
  Bars: '🍺',
  Transport: '🚗',
  Health: '💊',
  Retail: '🛍️',
  Utilities: '💡',
  Other: '💳',
};

function MerchantIcon({ tx }: { tx: Transaction }) {
  const [failed, setFailed] = useState(false);

  if (tx.merchantLogoUrl && !failed) {
    return (
      <img
        src={tx.merchantLogoUrl}
        alt=""
        className="w-8 h-8 rounded-full object-contain bg-dark-surface"
        onError={() => setFailed(true)}
      />
    );
  }

  return (
    <span className="w-8 h-8 rounded-full bg-dark-surface flex items-center justify-center text-base">
      {categoryIcons[tx.merchantCategory] ?? categoryIcons.Other}
    </span>
  );
}

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
      .filter((t) => t.status === TransactionStatus.Settled)
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
                className={`bg-dark-elevated rounded-lg px-4 py-3 flex items-center justify-between border border-border ${tx.status === TransactionStatus.Pending ? 'opacity-70 border-accent-amber/30' : ''} ${tx.status === TransactionStatus.Failed ? 'border-red-500/40 bg-red-500/5' : ''}`}
              >
                <div className="shrink-0 mr-3">
                  <MerchantIcon tx={tx} />
                </div>
                <div className="flex-1 min-w-0 mr-3">
                  <p className={`text-sm font-medium truncate ${tx.status === TransactionStatus.Failed ? 'text-red-400' : 'text-text-primary'}`}>
                    {tx.description}
                  </p>
                  {tx.status === TransactionStatus.Failed && tx.failureReason && (
                    <p className="text-xs text-red-400/80 mt-0.5">
                      {tx.failureReason}
                    </p>
                  )}
                  {tx.originalCurrency && tx.originalAmount != null && tx.exchangeRate != null && (
                    <p className="text-xs text-accent-amber mt-0.5">
                      {formatForeignCurrency(tx.originalAmount, tx.originalCurrency)}
                      {' · '}1 AUD = {tx.exchangeRate.toFixed(tx.exchangeRate >= 10 ? 0 : 2)} {tx.originalCurrency}
                      {tx.feeAmount != null && tx.feeAmount > 0 && (
                        <span className="text-text-muted"> · {formatCurrency(tx.feeAmount)} fee</span>
                      )}
                    </p>
                  )}
                  <p className="text-xs text-text-secondary mt-0.5">
                    {tx.merchantCategory} · {transactionTypeLabel[tx.transactionType]} ·{' '}
                    {new Date(tx.createdAt).toLocaleTimeString('en-AU', {
                      hour: '2-digit',
                      minute: '2-digit',
                    })}
                  </p>
                </div>
                <div className="text-right shrink-0">
                  {tx.status === TransactionStatus.Failed ? (
                    <p className="text-sm font-semibold text-red-400">Failed</p>
                  ) : (
                    <p className={`text-sm font-semibold ${txColor(tx)}`}>
                      {txSign(tx)}
                      {formatCurrency(Math.abs(tx.amount))}
                    </p>
                  )}
                  <p className="text-xs text-text-muted">
                    {tx.status === TransactionStatus.Pending ? (
                      <span className="text-accent-amber">Pending</span>
                    ) : tx.status === TransactionStatus.Failed ? (
                      <span className="text-red-400">Declined</span>
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
