import { Link } from 'react-router-dom';
import type { Account } from '../types';
import {
  accountTypeLabel,
  accountTypeBg,
  formatCurrency,
} from '../types';

interface Props {
  account: Account;
}

const gradientTop: Record<number, string> = {
  0: 'from-accent-cyan/60 to-accent-cyan/0',
  1: 'from-accent-teal/60 to-accent-teal/0',
  2: 'from-accent-coral/60 to-accent-coral/0',
  3: 'from-accent-amber/60 to-accent-amber/0',
};

export default function AccountCard({ account }: Props) {
  const isNegative = account.balance < 0;

  return (
    <Link
      to={`/accounts/${account.id}`}
      className="block bg-dark-elevated hover:bg-dark-elevated/80 rounded-xl overflow-hidden transition-colors border border-border"
    >
      <div className={`h-0.5 bg-gradient-to-r ${gradientTop[account.accountType] ?? ''}`} />
      <div className="p-4">
        <div className="flex items-center justify-between mb-2">
          <h3 className="font-semibold text-text-primary truncate mr-2">
            {account.name}
          </h3>
          <span
            className={`text-xs font-medium px-2 py-0.5 rounded-full ${accountTypeBg[account.accountType]}`}
          >
            {accountTypeLabel[account.accountType]}
          </span>
        </div>
        <div className="text-xs text-text-secondary mb-2">
          BSB {account.bsb} · {account.accountNumber}
        </div>
        <div
          className={`text-xl font-bold ${isNegative ? 'text-accent-coral' : 'text-accent-teal'}`}
        >
          {formatCurrency(account.balance)}
        </div>
      </div>
    </Link>
  );
}
