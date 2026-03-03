export interface Customer {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phone: string | null;
  dateOfBirth: string;
  createdAt: string;
  fullName: string;
  accountCount: number;
  activeAccountCount?: number;
}

export interface CustomerDetail extends Customer {
  accounts: Account[];
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
  Adjustment: 5,
  DirectDebit: 6,
} as const;
export type TransactionType = (typeof TransactionType)[keyof typeof TransactionType];

export interface Account {
  id: number;
  accountType: AccountType;
  bsb: string;
  accountNumber: string;
  name: string;
  balance: number;
  availableBalance?: number;
  isActive: boolean;
  createdAt: string;
}

export const TransactionStatus = {
  Pending: 0,
  Settled: 1,
  Reversed: 2,
} as const;
export type TransactionStatus = (typeof TransactionStatus)[keyof typeof TransactionStatus];

export interface Transaction {
  id: number;
  amount: number;
  description: string;
  transactionType: TransactionType;
  status: TransactionStatus;
  settledAt: string | null;
  createdAt: string;
}

export interface CustomerNote {
  id: number;
  content: string;
  createdAt: string;
  author: string;
}

export interface StaffSession {
  token: string;
  username: string;
  displayName: string;
  role: string;
}

export const accountTypeLabel: Record<AccountType, string> = {
  [AccountType.Transaction]: 'Transaction',
  [AccountType.Savings]: 'Savings',
  [AccountType.HomeLoan]: 'Home Loan',
  [AccountType.Offset]: 'Offset',
};

export const accountTypeBadge: Record<AccountType, string> = {
  [AccountType.Transaction]: 'bg-blue-100 text-blue-800',
  [AccountType.Savings]: 'bg-crm-highlight/50 text-crm-dark',
  [AccountType.HomeLoan]: 'bg-crm-warning/15 text-crm-warning',
  [AccountType.Offset]: 'bg-purple-100 text-purple-800',
};

export const transactionTypeLabel: Record<TransactionType, string> = {
  [TransactionType.Deposit]: 'Deposit',
  [TransactionType.Withdrawal]: 'Withdrawal',
  [TransactionType.Transfer]: 'Transfer',
  [TransactionType.Interest]: 'Interest',
  [TransactionType.Repayment]: 'Repayment',
  [TransactionType.Adjustment]: 'Adjustment',
  [TransactionType.DirectDebit]: 'Direct Debit',
};

export interface ScheduledPayment {
  id: number;
  accountId: number;
  payeeName: string;
  payeeBsb: string | null;
  payeeAccountNumber: string | null;
  payeeAccountId: number | null;
  amount: number;
  description: string | null;
  reference: string | null;
  frequency: string;
  startDate: string;
  endDate: string | null;
  nextDueDate: string;
  isActive: boolean;
  createdAt: string;
}

export function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('en-AU', {
    style: 'currency',
    currency: 'AUD',
  }).format(amount);
}

export function formatDate(date: string): string {
  return new Date(date).toLocaleDateString('en-AU', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
}

export function formatDateTime(date: string): string {
  return new Date(date).toLocaleString('en-AU', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}
