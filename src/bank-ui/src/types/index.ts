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
  DirectDebit: 6,
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
  availableBalance?: number;
  isActive?: boolean;
}

export const TransactionStatus = {
  Pending: 0,
  Settled: 1,
  Reversed: 2,
} as const;
export type TransactionStatus = (typeof TransactionStatus)[keyof typeof TransactionStatus];

export interface Transaction {
  id: string;
  amount: number;
  description: string;
  transactionType: TransactionType;
  status: TransactionStatus;
  settledAt: string | null;
  createdAt: string;
}

export const accountTypeLabel: Record<AccountType, string> = {
  [AccountType.Transaction]: 'Transaction',
  [AccountType.Savings]: 'Savings',
  [AccountType.HomeLoan]: 'Home Loan',
  [AccountType.Offset]: 'Offset',
};

export const accountTypeColor: Record<AccountType, string> = {
  [AccountType.Transaction]: 'text-accent-cyan',
  [AccountType.Savings]: 'text-accent-teal',
  [AccountType.HomeLoan]: 'text-accent-coral',
  [AccountType.Offset]: 'text-accent-amber',
};

export const accountTypeBg: Record<AccountType, string> = {
  [AccountType.Transaction]: 'bg-accent-cyan/15 text-accent-cyan',
  [AccountType.Savings]: 'bg-accent-teal/15 text-accent-teal',
  [AccountType.HomeLoan]: 'bg-accent-coral/15 text-accent-coral',
  [AccountType.Offset]: 'bg-accent-amber/15 text-accent-amber',
};

export const transactionTypeLabel: Record<TransactionType, string> = {
  [TransactionType.Deposit]: 'Deposit',
  [TransactionType.Withdrawal]: 'Withdrawal',
  [TransactionType.Transfer]: 'Transfer',
  [TransactionType.Interest]: 'Interest',
  [TransactionType.Repayment]: 'Repayment',
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

export const frequencyLabel: Record<string, string> = {
  OneOff: 'One-off',
  Weekly: 'Weekly',
  Fortnightly: 'Fortnightly',
  Monthly: 'Monthly',
  Quarterly: 'Quarterly',
  Yearly: 'Yearly',
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

export interface NudgeDto {
  id: number;
  message: string;
  cta: string;
  urgency: 'HIGH' | 'MEDIUM' | 'LOW';
  category: 'CASHFLOW' | 'SAVINGS' | 'SPENDING' | 'UPCOMING_PAYMENT';
}

export interface NudgeGenerateResult {
  generated: boolean;
  nudge: NudgeDto | null;
  reason: string | null;
}
