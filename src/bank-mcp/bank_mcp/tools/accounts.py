from datetime import date, timedelta

import asyncpg

from bank_mcp.db import fetch_all, fetch_one
from bank_mcp.models import AccountDetails, OffsetAccountInfo, BalanceSnapshot, ACCOUNT_TYPE_MAP


async def get_account_details(pool: asyncpg.Pool, account_id: int) -> AccountDetails:
    """Get full account details including available balance and offset accounts."""
    row = await fetch_one(
        pool,
        """
        SELECT a."Id", a."CustomerId", a."AccountType", a."Bsb", a."AccountNumber",
               a."Name", a."Balance", a."IsActive", a."LoanAmount", a."InterestRate",
               a."LoanTermMonths", a."HomeLoanAccountId",
               c."FirstName", c."LastName"
        FROM "Accounts" a
        JOIN "Customers" c ON c."Id" = a."CustomerId"
        WHERE a."Id" = $1
        """,
        account_id,
    )
    if row is None:
        raise ValueError(f"Account {account_id} not found")

    pending_row = await fetch_one(
        pool,
        """
        SELECT COALESCE(SUM("Amount"), 0) AS pending_debits
        FROM "Transactions"
        WHERE "AccountId" = $1 AND "Amount" < 0 AND "Status" = 'Pending'
        """,
        account_id,
    )
    # pending_debits is a negative sum (e.g. -150 for $150 in holds)
    # Adding it to the ledger balance reduces available balance correctly.
    pending_debits = pending_row["pending_debits"] if pending_row else 0
    available_balance = row["Balance"] + pending_debits

    offset_accounts: list[OffsetAccountInfo] = []
    if row["AccountType"] == 2:
        offset_rows = await fetch_all(
            pool,
            """
            SELECT "Id", "Name", "Balance"
            FROM "Accounts"
            WHERE "HomeLoanAccountId" = $1
            """,
            account_id,
        )
        offset_accounts = [
            OffsetAccountInfo(
                id=r["Id"],
                name=r["Name"],
                balance=f"{r['Balance']:.2f}",
            )
            for r in offset_rows
        ]

    return AccountDetails(
        id=row["Id"],
        customer_id=row["CustomerId"],
        customer_name=f"{row['FirstName']} {row['LastName']}",
        account_type=ACCOUNT_TYPE_MAP.get(row["AccountType"], "Unknown"),
        bsb=row["Bsb"],
        account_number=row["AccountNumber"],
        name=row["Name"],
        balance=f"{row['Balance']:.2f}",
        available_balance=f"{available_balance:.2f}",
        is_active=row["IsActive"],
        loan_amount=f"{row['LoanAmount']:.2f}" if row["LoanAmount"] is not None else None,
        interest_rate=f"{row['InterestRate']:.2f}" if row["InterestRate"] is not None else None,
        loan_term_months=row["LoanTermMonths"],
        offset_accounts=offset_accounts,
    )


async def get_balance_history(
    pool: asyncpg.Pool,
    account_id: int,
    from_date: date | None = None,
    to_date: date | None = None,
) -> list[BalanceSnapshot]:
    """Get EOD balance snapshots for an account over a date range."""
    if to_date is None:
        to_date = date.today()
    if from_date is None:
        from_date = to_date - timedelta(days=30)

    rows = await fetch_all(
        pool,
        """
        SELECT "SnapshotDate", "LedgerBalance", "AvailableBalance"
        FROM "AccountBalanceSnapshots"
        WHERE "AccountId" = $1 AND "SnapshotDate" >= $2 AND "SnapshotDate" <= $3
        ORDER BY "SnapshotDate" ASC
        """,
        account_id,
        from_date,
        to_date,
    )

    return [
        BalanceSnapshot(
            snapshot_date=r["SnapshotDate"],
            ledger_balance=f"{r['LedgerBalance']:.2f}",
            available_balance=f"{r['AvailableBalance']:.2f}",
        )
        for r in rows
    ]
