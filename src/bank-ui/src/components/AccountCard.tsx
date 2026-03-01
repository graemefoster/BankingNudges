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

  return (
    <Link
      to={`/accounts/${account.id}`}
      className="block bg-dark-card hover:bg-dark-card-hover rounded-xl p-4 transition-colors"
    >
      <div className="flex items-center justify-between mb-2">
        <h3 className="font-semibold text-white truncate mr-2">
          {account.name}
        </h3>
        <span
          className={`text-xs font-medium px-2 py-0.5 rounded-full ${accountTypeBg[account.accountType]}`}
        >
          {accountTypeLabel[account.accountType]}
        </span>
      </div>
      <div className="text-xs text-gray-400 mb-2">
        BSB {account.bsb} · {account.accountNumber}
      </div>
      <div
        className={`text-xl font-bold ${isNegative ? 'text-brand-red' : 'text-brand-mint'}`}
      >
        {formatCurrency(account.balance)}
      </div>
    </Link>
  );
}
