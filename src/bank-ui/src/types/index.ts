export interface Customer {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  fullName: string;
  accountCount: number;
}

export const AccountType = {
  Transaction: 0,
  Savings: 1,
  HomeLoan: 2,
  Offset: 3,
} as const;
export type AccountType = (typeof AccountType)[keyof typeof AccountType];

export const TransactionType = {
  Deposit: 0,
  Withdrawal: 1,
  Transfer: 2,
  Interest: 3,
  Repayment: 4,
} as const;
export type TransactionType = (typeof TransactionType)[keyof typeof TransactionType];

export interface Account {
  id: string;
  customerId: string;
  accountType: AccountType;
  bsb: string;
  accountNumber: string;
  name: string;
  balance: number;
}

export interface Transaction {
  id: string;
  amount: number;
  description: string;
  transactionType: TransactionType;
  balanceAfter: number;
  createdAt: string;
}

export const accountTypeLabel: Record<AccountType, string> = {
  [AccountType.Transaction]: 'Transaction',
  [AccountType.Savings]: 'Savings',
  [AccountType.HomeLoan]: 'Home Loan',
  [AccountType.Offset]: 'Offset',
};

export const accountTypeColor: Record<AccountType, string> = {
  [AccountType.Transaction]: 'text-brand-blue',
  [AccountType.Savings]: 'text-brand-mint',
  [AccountType.HomeLoan]: 'text-brand-red',
  [AccountType.Offset]: 'text-brand-purple',
};

export const accountTypeBg: Record<AccountType, string> = {
  [AccountType.Transaction]: 'bg-brand-blue/15 text-brand-blue',
  [AccountType.Savings]: 'bg-brand-mint/40 text-emerald-700',
  [AccountType.HomeLoan]: 'bg-brand-red/15 text-brand-red',
  [AccountType.Offset]: 'bg-brand-purple/15 text-brand-purple',
};

export const transactionTypeLabel: Record<TransactionType, string> = {
  [TransactionType.Deposit]: 'Deposit',
  [TransactionType.Withdrawal]: 'Withdrawal',
  [TransactionType.Transfer]: 'Transfer',
  [TransactionType.Interest]: 'Interest',
  [TransactionType.Repayment]: 'Repayment',
};

export function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('en-AU', {
    style: 'currency',
    currency: 'AUD',
  }).format(amount);
}

export interface PayeeLookup {
  name: string;
  accountType: AccountType;
  accountTypeName: string;
  customerName: string;
}
