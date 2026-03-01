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

export default function AccountCard({ account }: Props) {
  const isNegative = account.balance < 0;

  const borderColor: Record<number, string> = {
    0: 'border-l-brand-blue',
    1: 'border-l-brand-mint',
    2: 'border-l-brand-red',
    3: 'border-l-brand-purple',
  };

  return (
    <Link
      to={`/accounts/${account.id}`}
      className={`block bg-light-card hover:bg-light-card-hover rounded-xl p-4 transition-colors shadow-sm border-l-4 ${borderColor[account.accountType] ?? ''}`}
    >
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
        className={`text-xl font-bold ${isNegative ? 'text-brand-red' : 'text-emerald-600'}`}
      >
        {formatCurrency(account.balance)}
      </div>
    </Link>
  );
}
