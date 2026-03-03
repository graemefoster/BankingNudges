from datetime import date, timedelta
from decimal import Decimal

from asyncpg import Pool

from bank_mcp.db import fetch_all
from bank_mcp.models import (
    SpendingSummary,
    SpendingCategory,
    InterestSummary,
    InterestAccrualRecord,
    TRANSACTION_TYPE_MAP,
)


async def get_spending_summary(
    pool: Pool,
    account_id: int,
    from_date: date | None = None,
    to_date: date | None = None,
) -> SpendingSummary:
    """Get spending breakdown by transaction type over a period."""
    today = date.today()
    if from_date is None:
        from_date = today - timedelta(days=30)
    if to_date is None:
        to_date = today

    rows = await fetch_all(
        pool,
        """
        SELECT "TransactionType",
               SUM("Amount") AS total_amount,
               COUNT(*)      AS transaction_count
        FROM "Transactions"
        WHERE "AccountId" = $1
          AND "CreatedAt" >= $2
          AND "CreatedAt" < ($3 + INTERVAL '1 day')
          AND "Status" != 'Reversed'
        GROUP BY "TransactionType"
        """,
        account_id,
        from_date,
        to_date,
    )

    categories: list[SpendingCategory] = []
    total_spent = Decimal("0")
    total_received = Decimal("0")

    for row in rows:
        amount = Decimal(str(row["total_amount"]))
        if amount < 0:
            total_spent += abs(amount)
        else:
            total_received += amount

        categories.append(
            SpendingCategory(
                transaction_type=TRANSACTION_TYPE_MAP.get(
                    row["TransactionType"], f"Unknown({row['TransactionType']})"
                ),
                total_amount=f"{abs(amount):.2f}",
                transaction_count=row["transaction_count"],
            )
        )

    return SpendingSummary(
        account_id=account_id,
        from_date=from_date,
        to_date=to_date,
        categories=categories,
        total_spent=f"{total_spent:.2f}",
        total_received=f"{total_received:.2f}",
    )


async def get_interest_summary(
    pool: Pool,
    account_id: int,
    from_date: date | None = None,
    to_date: date | None = None,
) -> InterestSummary:
    """Get interest accrual summary for an account."""
    today = date.today()
    if from_date is None:
        from_date = today - timedelta(days=30)
    if to_date is None:
        to_date = today

    rows = await fetch_all(
        pool,
        """
        SELECT "AccrualDate", "DailyAmount", "Posted"
        FROM "InterestAccruals"
        WHERE "AccountId" = $1
          AND "AccrualDate" >= $2
          AND "AccrualDate" <= $3
        ORDER BY "AccrualDate" ASC
        """,
        account_id,
        from_date,
        to_date,
    )

    total_interest = Decimal("0")
    accruals: list[InterestAccrualRecord] = []

    for row in rows:
        amount = Decimal(str(row["DailyAmount"]))
        total_interest += amount
        accruals.append(
            InterestAccrualRecord(
                accrual_date=row["AccrualDate"],
                daily_amount=f"{amount:.2f}",
                posted=row["Posted"],
            )
        )

    return InterestSummary(
        account_id=account_id,
        from_date=from_date,
        to_date=to_date,
        total_interest=f"{total_interest:.2f}",
        accrual_count=len(accruals),
        accruals=accruals,
    )
