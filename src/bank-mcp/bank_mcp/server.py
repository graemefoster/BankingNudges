from __future__ import annotations

import sys
from contextlib import asynccontextmanager

from fastmcp import FastMCP
from fastmcp.server.context import Context
from fastmcp.dependencies import CurrentContext

from bank_mcp.db import create_pool
from bank_mcp.tools.customers import search_customers, get_customer_profile
from bank_mcp.tools.accounts import get_account_details, get_balance_history
from bank_mcp.tools.transactions import (
    get_transactions,
    search_transactions,
    find_similar_transactions,
    find_potential_duplicate_charges,
    get_pending_transactions,
)
from bank_mcp.tools.payments import get_scheduled_payments, get_payment_execution_history
from bank_mcp.tools.notes import get_customer_notes
from bank_mcp.tools.analytics import get_spending_summary, get_interest_summary

from datetime import date
from typing import Optional


def _parse_date(value: str | None) -> date | None:
    if not value:
        return None
    try:
        return date.fromisoformat(value)
    except ValueError:
        raise ValueError(f"Invalid date format '{value}' — expected YYYY-MM-DD")


@asynccontextmanager
async def lifespan(server: FastMCP):
    pool = await create_pool()
    try:
        yield {"pool": pool}
    finally:
        await pool.close()


mcp = FastMCP(
    "Bank of Graeme — Contact Centre",
    instructions=(
        "You are assisting a contact centre agent at Bank of Graeme. "
        "Use these tools to look up customers, investigate transactions, "
        "review scheduled payments, and answer account queries. "
        "All data is read-only. Monetary amounts are in AUD."
    ),
    lifespan=lifespan,
)


# ---------------------------------------------------------------------------
# Customer Lookup
# ---------------------------------------------------------------------------

@mcp.tool
async def search_customers_tool(
    query: str,
    limit: int = 10,
    ctx: Context = CurrentContext,
) -> dict:
    """Search for a customer by name, email, phone number, or account number.
    This is typically the first step when a customer calls in.

    Args:
        query: Search term — can be a name, email address, phone number, BSB+account number, or just account number.
        limit: Maximum number of results to return (default 10).
    """
    pool = ctx.lifespan_context["pool"]
    results = await search_customers(pool, query, limit)
    return {"customers": [r.model_dump(mode="json") for r in results]}


@mcp.tool
async def get_customer_profile_tool(customer_id: int, ctx: Context = CurrentContext) -> dict:
    """Get a customer's full profile including all their accounts and net financial position.

    Args:
        customer_id: The customer's ID (from search_customers).
    """
    pool = ctx.lifespan_context["pool"]
    try:
        p = await get_customer_profile(pool, customer_id)
    except ValueError as e:
        return {"error": str(e)}
    return p.model_dump(mode="json")


# ---------------------------------------------------------------------------
# Account Details
# ---------------------------------------------------------------------------

@mcp.tool
async def get_account_details_tool(account_id: int, ctx: Context = CurrentContext) -> dict:
    """Get detailed information about a specific account including balance, available balance, loan details, and offset accounts.

    Args:
        account_id: The account ID (from customer profile).
    """
    pool = ctx.lifespan_context["pool"]
    try:
        a = await get_account_details(pool, account_id)
    except ValueError as e:
        return {"error": str(e)}
    return a.model_dump(mode="json")


@mcp.tool
async def get_balance_history_tool(
    account_id: int,
    from_date: Optional[str] = None,
    to_date: Optional[str] = None,
    ctx: Context = CurrentContext,
) -> dict:
    """Get the end-of-day balance history for an account over a date range.
    Useful for seeing how a balance has changed over time.

    Args:
        account_id: The account ID.
        from_date: Start date (YYYY-MM-DD). Defaults to 30 days ago.
        to_date: End date (YYYY-MM-DD). Defaults to today.
    """
    pool = ctx.lifespan_context["pool"]
    fd = _parse_date(from_date)
    td = _parse_date(to_date)
    snapshots = await get_balance_history(pool, account_id, fd, td)
    return {"snapshots": [s.model_dump(mode="json") for s in snapshots]}


# ---------------------------------------------------------------------------
# Transaction Investigation
# ---------------------------------------------------------------------------

@mcp.tool
async def get_transactions_tool(
    account_id: int,
    from_date: Optional[str] = None,
    to_date: Optional[str] = None,
    transaction_type: Optional[str] = None,
    min_amount: Optional[float] = None,
    max_amount: Optional[float] = None,
    description: Optional[str] = None,
    status: Optional[str] = None,
    page: int = 1,
    page_size: int = 20,
    ctx: Context = CurrentContext,
) -> dict:
    """Get a filtered, paginated list of transactions for an account.

    Args:
        account_id: The account ID.
        from_date: Start date filter (YYYY-MM-DD).
        to_date: End date filter (YYYY-MM-DD).
        transaction_type: Filter by type — one of: Deposit, Withdrawal, Transfer, Interest, Repayment, Adjustment, DirectDebit.
        min_amount: Minimum absolute amount filter.
        max_amount: Maximum absolute amount filter.
        description: Text search filter on transaction description (case-insensitive).
        status: Filter by status — Pending, Settled, or Reversed. By default, Reversed transactions are excluded.
        page: Page number (default 1).
        page_size: Results per page (default 20).
    """
    pool = ctx.lifespan_context["pool"]
    fd = _parse_date(from_date)
    td = _parse_date(to_date)
    result = await get_transactions(
        pool, account_id, fd, td, transaction_type,
        min_amount, max_amount, description, status, page, page_size,
    )
    return result.model_dump(mode="json")


@mcp.tool
async def search_transactions_tool(
    customer_id: int,
    description_query: str,
    from_date: Optional[str] = None,
    to_date: Optional[str] = None,
    limit: int = 20,
    ctx: Context = CurrentContext,
) -> dict:
    """Search for transactions across ALL of a customer's accounts by description.
    Useful when a customer says "I see a charge from X" but doesn't know which account.

    Args:
        customer_id: The customer's ID.
        description_query: Text to search for in transaction descriptions (case-insensitive).
        from_date: Start date filter (YYYY-MM-DD).
        to_date: End date filter (YYYY-MM-DD).
        limit: Maximum results (default 20).
    """
    pool = ctx.lifespan_context["pool"]
    fd = _parse_date(from_date)
    td = _parse_date(to_date)
    results = await search_transactions(pool, customer_id, description_query, fd, td, limit)
    return {"transactions": [r.model_dump(mode="json") for r in results]}


@mcp.tool
async def find_similar_transactions_tool(
    transaction_id: int,
    similarity_window_days: int = 90,
    amount_tolerance_pct: float = 10.0,
    ctx: Context = CurrentContext,
) -> dict:
    """Find transactions similar to a given transaction — useful for dispute investigation.
    Matches by similar description (merchant name) and/or similar amount.

    Args:
        transaction_id: The ID of the transaction to find similar ones for.
        similarity_window_days: How far back/forward to search (default 90 days).
        amount_tolerance_pct: How close the amount needs to be, as a percentage (default 10%).
    """
    pool = ctx.lifespan_context["pool"]
    try:
        results = await find_similar_transactions(
            pool, transaction_id, similarity_window_days, amount_tolerance_pct
        )
    except ValueError as e:
        return {"error": str(e)}
    return {"similar_transactions": [r.model_dump(mode="json") for r in results]}


@mcp.tool
async def find_potential_duplicate_charges_tool(
    account_id: int,
    lookback_days: int = 30,
    time_window_days: int = 3,
    ctx: Context = CurrentContext,
) -> dict:
    """Detect potential duplicate charges on an account.
    Finds transactions with the same amount and similar description within a short time window.

    Args:
        account_id: The account ID to check.
        lookback_days: How many days back to look (default 30).
        time_window_days: Maximum days apart for transactions to be considered duplicates (default 3).
    """
    pool = ctx.lifespan_context["pool"]
    groups = await find_potential_duplicate_charges(
        pool, account_id, lookback_days, time_window_days
    )
    return {"duplicate_groups": [g.model_dump(mode="json") for g in groups]}


@mcp.tool
async def get_pending_transactions_tool(account_id: int, ctx: Context = CurrentContext) -> dict:
    """Get all pending (unsettled) transactions for an account.
    Explains why the available balance differs from the ledger balance.

    Args:
        account_id: The account ID.
    """
    pool = ctx.lifespan_context["pool"]
    results = await get_pending_transactions(pool, account_id)
    return {"pending_transactions": [r.model_dump(mode="json") for r in results]}


# ---------------------------------------------------------------------------
# Scheduled Payments / Direct Debits
# ---------------------------------------------------------------------------

@mcp.tool
async def get_scheduled_payments_tool(
    account_id: int,
    include_inactive: bool = False,
    ctx: Context = CurrentContext,
) -> dict:
    """Get all scheduled payments (direct debits) for an account.

    Args:
        account_id: The account ID.
        include_inactive: Whether to include cancelled/expired payments (default false).
    """
    pool = ctx.lifespan_context["pool"]
    results = await get_scheduled_payments(pool, account_id, include_inactive)
    return {"scheduled_payments": [r.model_dump(mode="json") for r in results]}


@mcp.tool
async def get_payment_execution_history_tool(
    scheduled_payment_id: int,
    limit: int = 10,
    ctx: Context = CurrentContext,
) -> dict:
    """Get recent execution history for a specific scheduled payment.
    Shows whether payments went through or were declined (insufficient funds).

    Args:
        scheduled_payment_id: The scheduled payment ID (from get_scheduled_payments).
        limit: Maximum results (default 10).
    """
    pool = ctx.lifespan_context["pool"]
    try:
        results = await get_payment_execution_history(pool, scheduled_payment_id, limit)
    except ValueError as e:
        return {"error": str(e)}
    return {"executions": [r.model_dump(mode="json") for r in results]}


# ---------------------------------------------------------------------------
# CRM / Notes
# ---------------------------------------------------------------------------

@mcp.tool
async def get_customer_notes_tool(
    customer_id: int,
    limit: int = 20,
    ctx: Context = CurrentContext,
) -> dict:
    """Get previous interaction notes for a customer, written by staff.
    Provides context before speaking with the customer.

    Args:
        customer_id: The customer's ID.
        limit: Maximum notes to return (default 20).
    """
    pool = ctx.lifespan_context["pool"]
    results = await get_customer_notes(pool, customer_id, limit)
    return {"notes": [r.model_dump(mode="json") for r in results]}


# ---------------------------------------------------------------------------
# Analytics
# ---------------------------------------------------------------------------

@mcp.tool
async def get_spending_summary_tool(
    account_id: int,
    from_date: Optional[str] = None,
    to_date: Optional[str] = None,
    ctx: Context = CurrentContext,
) -> dict:
    """Get a spending breakdown by transaction type for an account.

    Args:
        account_id: The account ID.
        from_date: Start date (YYYY-MM-DD). Defaults to 30 days ago.
        to_date: End date (YYYY-MM-DD). Defaults to today.
    """
    pool = ctx.lifespan_context["pool"]
    fd = _parse_date(from_date)
    td = _parse_date(to_date)
    s = await get_spending_summary(pool, account_id, fd, td)
    return s.model_dump(mode="json")


@mcp.tool
async def get_interest_summary_tool(
    account_id: int,
    from_date: Optional[str] = None,
    to_date: Optional[str] = None,
    ctx: Context = CurrentContext,
) -> dict:
    """Get interest accrual summary for an account.
    Shows daily interest charges (for loans) or earnings (for savings).

    Args:
        account_id: The account ID.
        from_date: Start date (YYYY-MM-DD). Defaults to 30 days ago.
        to_date: End date (YYYY-MM-DD). Defaults to today.
    """
    pool = ctx.lifespan_context["pool"]
    fd = _parse_date(from_date)
    td = _parse_date(to_date)
    s = await get_interest_summary(pool, account_id, fd, td)
    return s.model_dump(mode="json")


# ---------------------------------------------------------------------------
# Entrypoint
# ---------------------------------------------------------------------------

def main():
    transport = "streamable-http"
    if "--stdio" in sys.argv:
        transport = "stdio"
    mcp.run(transport=transport, host="127.0.0.1", port=8080)


if __name__ == "__main__":
    main()
