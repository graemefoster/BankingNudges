from __future__ import annotations

import asyncpg

from bank_mcp.db import fetch_all, fetch_one
from bank_mcp.models import ScheduledPaymentInfo, PaymentExecution


async def get_scheduled_payments(
    pool: asyncpg.Pool,
    account_id: int,
    include_inactive: bool = False,
) -> list[ScheduledPaymentInfo]:
    """Get all scheduled payments for an account."""

    if include_inactive:
        query = """
            SELECT "Id", "AccountId", "PayeeName", "PayeeBsb",
                   "PayeeAccountNumber", "Amount", "Description",
                   "Reference", "Frequency", "StartDate", "EndDate",
                   "NextDueDate", "IsActive"
            FROM "ScheduledPayments"
            WHERE "AccountId" = $1
            ORDER BY "IsActive" DESC, "NextDueDate" ASC
        """
        rows = await fetch_all(pool, query, account_id)
    else:
        query = """
            SELECT "Id", "AccountId", "PayeeName", "PayeeBsb",
                   "PayeeAccountNumber", "Amount", "Description",
                   "Reference", "Frequency", "StartDate", "EndDate",
                   "NextDueDate", "IsActive"
            FROM "ScheduledPayments"
            WHERE "AccountId" = $1 AND "IsActive" = true
            ORDER BY "IsActive" DESC, "NextDueDate" ASC
        """
        rows = await fetch_all(pool, query, account_id)

    return [
        ScheduledPaymentInfo(
            id=r["Id"],
            account_id=r["AccountId"],
            payee_name=r["PayeeName"],
            payee_bsb=r["PayeeBsb"],
            payee_account_number=r["PayeeAccountNumber"],
            amount=f"{r['Amount']:.2f}",
            description=r["Description"],
            reference=r["Reference"],
            frequency=r["Frequency"],
            start_date=r["StartDate"],
            end_date=r["EndDate"],
            next_due_date=r["NextDueDate"],
            is_active=r["IsActive"],
        )
        for r in rows
    ]


async def get_payment_execution_history(
    pool: asyncpg.Pool,
    scheduled_payment_id: int,
    limit: int = 10,
) -> list[PaymentExecution]:
    """Get recent transaction history for a specific scheduled payment."""

    sp_query = """
        SELECT "AccountId", "PayeeName", "Amount"
        FROM "ScheduledPayments"
        WHERE "Id" = $1
    """
    sp = await fetch_one(pool, sp_query, scheduled_payment_id)
    if sp is None:
        raise ValueError(f"Scheduled payment {scheduled_payment_id} not found")

    tx_query = """
        SELECT "Id", "Amount", "Description", "Status", "CreatedAt"
        FROM "Transactions"
        WHERE "AccountId" = $1
          AND "TransactionType" = 6
          AND "Description" ILIKE '%' || $2 || '%'
        ORDER BY "CreatedAt" DESC
        LIMIT $3
    """
    rows = await fetch_all(pool, tx_query, sp["AccountId"], sp["PayeeName"], limit)

    return [
        PaymentExecution(
            transaction_id=r["Id"],
            amount=f"{r['Amount']:.2f}",
            description=r["Description"],
            status=r["Status"],
            created_at=r["CreatedAt"],
            was_declined=r["Status"] == "Reversed",
        )
        for r in rows
    ]
