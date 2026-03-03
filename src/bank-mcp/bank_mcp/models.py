from __future__ import annotations

from datetime import date, datetime
from decimal import Decimal
from enum import Enum
from typing import Optional

from pydantic import BaseModel, ConfigDict


class AccountType(str, Enum):
    transaction = "Transaction"
    savings = "Savings"
    home_loan = "HomeLoan"
    offset = "Offset"


ACCOUNT_TYPE_MAP = {0: "Transaction", 1: "Savings", 2: "HomeLoan", 3: "Offset"}


class TransactionType(str, Enum):
    deposit = "Deposit"
    withdrawal = "Withdrawal"
    transfer = "Transfer"
    interest = "Interest"
    repayment = "Repayment"
    adjustment = "Adjustment"
    direct_debit = "DirectDebit"


TRANSACTION_TYPE_MAP = {
    0: "Deposit",
    1: "Withdrawal",
    2: "Transfer",
    3: "Interest",
    4: "Repayment",
    5: "Adjustment",
    6: "DirectDebit",
}


class Frequency(str, Enum):
    one_off = "OneOff"
    weekly = "Weekly"
    fortnightly = "Fortnightly"
    monthly = "Monthly"
    quarterly = "Quarterly"
    yearly = "Yearly"


# ---------- Response models ----------

class CustomerSummary(BaseModel):
    model_config = ConfigDict(from_attributes=True)

    id: int
    first_name: str
    last_name: str
    email: str
    phone: Optional[str] = None
    date_of_birth: date


class AccountSummary(BaseModel):
    model_config = ConfigDict(from_attributes=True)

    id: int
    account_type: str
    bsb: str
    account_number: str
    name: str
    balance: str
    is_active: bool


class CustomerProfile(BaseModel):
    id: int
    first_name: str
    last_name: str
    email: str
    phone: Optional[str] = None
    date_of_birth: date
    created_at: datetime
    accounts: list[AccountSummary]
    net_position: str


class OffsetAccountInfo(BaseModel):
    id: int
    name: str
    balance: str


class AccountDetails(BaseModel):
    id: int
    customer_id: int
    customer_name: str
    account_type: str
    bsb: str
    account_number: str
    name: str
    balance: str
    available_balance: str
    is_active: bool
    loan_amount: Optional[str] = None
    interest_rate: Optional[str] = None
    loan_term_months: Optional[int] = None
    offset_accounts: list[OffsetAccountInfo] = []


class TransactionRecord(BaseModel):
    id: int
    account_id: int
    amount: str
    description: str
    transaction_type: str
    status: str
    settled_at: Optional[datetime] = None
    created_at: datetime


class TransactionPage(BaseModel):
    transactions: list[TransactionRecord]
    total: int
    page: int
    page_size: int


class SimilarTransaction(BaseModel):
    id: int
    account_id: int
    account_name: str
    amount: str
    description: str
    transaction_type: str
    status: str
    created_at: datetime
    similarity_reason: str


class DuplicateGroup(BaseModel):
    amount: str
    description_pattern: str
    transactions: list[TransactionRecord]


class ScheduledPaymentInfo(BaseModel):
    id: int
    account_id: int
    payee_name: str
    payee_bsb: Optional[str] = None
    payee_account_number: Optional[str] = None
    amount: str
    description: Optional[str] = None
    reference: Optional[str] = None
    frequency: str
    start_date: date
    end_date: Optional[date] = None
    next_due_date: date
    is_active: bool


class PaymentExecution(BaseModel):
    transaction_id: int
    amount: str
    description: str
    status: str
    created_at: datetime
    was_declined: bool


class CustomerNote(BaseModel):
    id: int
    content: str
    author: str
    created_at: datetime


class BalanceSnapshot(BaseModel):
    snapshot_date: date
    ledger_balance: str
    available_balance: str


class SpendingCategory(BaseModel):
    transaction_type: str
    total_amount: str
    transaction_count: int


class SpendingSummary(BaseModel):
    account_id: int
    from_date: date
    to_date: date
    categories: list[SpendingCategory]
    total_spent: str
    total_received: str


class InterestAccrualRecord(BaseModel):
    accrual_date: date
    daily_amount: str
    posted: bool


class InterestSummary(BaseModel):
    account_id: int
    from_date: date
    to_date: date
    total_interest: str
    accrual_count: int
    accruals: list[InterestAccrualRecord]
