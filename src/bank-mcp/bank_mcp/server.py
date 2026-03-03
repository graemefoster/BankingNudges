from __future__ import annotations

import sys
from contextlib import asynccontextmanager

from fastmcp import FastMCP

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
) -> str:
    """Search for a customer by name, email, phone number, or account number.
    This is typically the first step when a customer calls in.

    Args:
        query: Search term — can be a name, email address, phone number, BSB+account number, or just account number.
        limit: Maximum number of results to return (default 10).
    """
    pool = mcp.get_context().lifespan_context["pool"]
    results = await search_customers(pool, query, limit)
    if not results:
        return "No customers found matching that query."
    lines = [f"Found {len(results)} customer(s):\n"]
    for c in results:
        lines.append(
            f"  ID {c.id}: {c.first_name} {c.last_name} | {c.email} | "
            f"Phone: {c.phone or 'N/A'} | DOB: {c.date_of_birth}"
        )
    return "\n".join(lines)


@mcp.tool
async def get_customer_profile_tool(customer_id: int) -> str:
    """Get a customer's full profile including all their accounts and net financial position.

    Args:
        customer_id: The customer's ID (from search_customers).
    """
    pool = mcp.get_context().lifespan_context["pool"]
    try:
        p = await get_customer_profile(pool, customer_id)
    except ValueError as e:
        return str(e)
    lines = [
        f"Customer: {p.first_name} {p.last_name} (ID {p.id})",
        f"Email: {p.email} | Phone: {p.phone or 'N/A'} | DOB: {p.date_of_birth}",
        f"Member since: {p.created_at:%Y-%m-%d}",
        f"Net position: ${p.net_position}",
        f"\nAccounts ({len(p.accounts)}):",
    ]
    for a in p.accounts:
        status = "Active" if a.is_active else "Closed"
        lines.append(
            f"  [{a.id}] {a.name} ({a.account_type}) — "
            f"BSB {a.bsb} Acct {a.account_number} — "
            f"Balance: ${a.balance} [{status}]"
        )
    return "\n".join(lines)


# ---------------------------------------------------------------------------
# Account Details
# ---------------------------------------------------------------------------

@mcp.tool
async def get_account_details_tool(account_id: int) -> str:
    """Get detailed information about a specific account including balance, available balance, loan details, and offset accounts.

    Args:
        account_id: The account ID (from customer profile).
    """
    pool = mcp.get_context().lifespan_context["pool"]
    try:
        a = await get_account_details(pool, account_id)
    except ValueError as e:
        return str(e)
    lines = [
        f"Account: {a.name} (ID {a.id})",
        f"Type: {a.account_type} | BSB: {a.bsb} | Number: {a.account_number}",
        f"Customer: {a.customer_name} (ID {a.customer_id})",
        f"Status: {'Active' if a.is_active else 'Closed'}",
        f"Ledger balance: ${a.balance}",
        f"Available balance: ${a.available_balance}",
    ]
    if a.loan_amount:
        lines.append(f"Loan amount: ${a.loan_amount}")
        lines.append(f"Interest rate: {a.interest_rate}%")
        lines.append(f"Loan term: {a.loan_term_months} months")
    if a.offset_accounts:
        lines.append(f"\nOffset accounts ({len(a.offset_accounts)}):")
        for o in a.offset_accounts:
            lines.append(f"  [{o.id}] {o.name} — Balance: ${o.balance}")
    return "\n".join(lines)


@mcp.tool
async def get_balance_history_tool(
    account_id: int,
    from_date: Optional[str] = None,
    to_date: Optional[str] = None,
) -> str:
    """Get the end-of-day balance history for an account over a date range.
    Useful for seeing how a balance has changed over time.

    Args:
        account_id: The account ID.
        from_date: Start date (YYYY-MM-DD). Defaults to 30 days ago.
        to_date: End date (YYYY-MM-DD). Defaults to today.
    """
    pool = mcp.get_context().lifespan_context["pool"]
    fd = _parse_date(from_date)
    td = _parse_date(to_date)
    snapshots = await get_balance_history(pool, account_id, fd, td)
    if not snapshots:
        return "No balance snapshots found for this account in the given date range."
    lines = [f"Balance history ({len(snapshots)} days):\n"]
    lines.append(f"  {'Date':<12} {'Ledger':>14} {'Available':>14}")
    lines.append(f"  {'─'*12} {'─'*14} {'─'*14}")
    for s in snapshots:
        lines.append(
            f"  {s.snapshot_date}  ${s.ledger_balance:>12}  ${s.available_balance:>12}"
        )
    return "\n".join(lines)


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
) -> str:
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
    pool = mcp.get_context().lifespan_context["pool"]
    fd = _parse_date(from_date)
    td = _parse_date(to_date)
    result = await get_transactions(
        pool, account_id, fd, td, transaction_type,
        min_amount, max_amount, description, status, page, page_size,
    )
    if not result.transactions:
        return "No transactions found matching the filters."
    lines = [f"Transactions (page {result.page}, {len(result.transactions)} of {result.total} total):\n"]
    for t in result.transactions:
        settled = f" (settled {t.settled_at:%Y-%m-%d})" if t.settled_at else ""
        lines.append(
            f"  [{t.id}] {t.created_at:%Y-%m-%d %H:%M} | {t.transaction_type:<12} | "
            f"${t.amount:>10} | {t.status:<8} | {t.description}{settled}"
        )
    return "\n".join(lines)


@mcp.tool
async def search_transactions_tool(
    customer_id: int,
    description_query: str,
    from_date: Optional[str] = None,
    to_date: Optional[str] = None,
    limit: int = 20,
) -> str:
    """Search for transactions across ALL of a customer's accounts by description.
    Useful when a customer says "I see a charge from X" but doesn't know which account.

    Args:
        customer_id: The customer's ID.
        description_query: Text to search for in transaction descriptions (case-insensitive).
        from_date: Start date filter (YYYY-MM-DD).
        to_date: End date filter (YYYY-MM-DD).
        limit: Maximum results (default 20).
    """
    pool = mcp.get_context().lifespan_context["pool"]
    fd = _parse_date(from_date)
    td = _parse_date(to_date)
    results = await search_transactions(pool, customer_id, description_query, fd, td, limit)
    if not results:
        return f"No transactions found matching '{description_query}' across this customer's accounts."
    lines = [f"Found {len(results)} transaction(s) matching '{description_query}':\n"]
    for t in results:
        lines.append(
            f"  [{t.id}] Acct {t.account_id} | {t.created_at:%Y-%m-%d %H:%M} | "
            f"{t.transaction_type:<12} | ${t.amount:>10} | {t.status} | {t.description}"
        )
    return "\n".join(lines)


@mcp.tool
async def find_similar_transactions_tool(
    transaction_id: int,
    similarity_window_days: int = 90,
    amount_tolerance_pct: float = 10.0,
) -> str:
    """Find transactions similar to a given transaction — useful for dispute investigation.
    Matches by similar description (merchant name) and/or similar amount.

    Args:
        transaction_id: The ID of the transaction to find similar ones for.
        similarity_window_days: How far back/forward to search (default 90 days).
        amount_tolerance_pct: How close the amount needs to be, as a percentage (default 10%).
    """
    pool = mcp.get_context().lifespan_context["pool"]
    try:
        results = await find_similar_transactions(
            pool, transaction_id, similarity_window_days, amount_tolerance_pct
        )
    except ValueError as e:
        return str(e)
    if not results:
        return "No similar transactions found."
    lines = [f"Found {len(results)} similar transaction(s):\n"]
    for t in results:
        lines.append(
            f"  [{t.id}] Acct {t.account_id} ({t.account_name}) | "
            f"{t.created_at:%Y-%m-%d %H:%M} | ${t.amount:>10} | {t.status} | "
            f"{t.description} — Reason: {t.similarity_reason}"
        )
    return "\n".join(lines)


@mcp.tool
async def find_potential_duplicate_charges_tool(
    account_id: int,
    lookback_days: int = 30,
    time_window_days: int = 3,
) -> str:
    """Detect potential duplicate charges on an account.
    Finds transactions with the same amount and similar description within a short time window.

    Args:
        account_id: The account ID to check.
        lookback_days: How many days back to look (default 30).
        time_window_days: Maximum days apart for transactions to be considered duplicates (default 3).
    """
    pool = mcp.get_context().lifespan_context["pool"]
    groups = await find_potential_duplicate_charges(
        pool, account_id, lookback_days, time_window_days
    )
    if not groups:
        return "No potential duplicate charges detected."
    lines = [f"Found {len(groups)} potential duplicate group(s):\n"]
    for g in groups:
        lines.append(f"  Amount: ${g.amount} | Pattern: {g.description_pattern}")
        for t in g.transactions:
            lines.append(
                f"    [{t.id}] {t.created_at:%Y-%m-%d %H:%M} | {t.description} | {t.status}"
            )
        lines.append("")
    return "\n".join(lines)


@mcp.tool
async def get_pending_transactions_tool(account_id: int) -> str:
    """Get all pending (unsettled) transactions for an account.
    Explains why the available balance differs from the ledger balance.

    Args:
        account_id: The account ID.
    """
    pool = mcp.get_context().lifespan_context["pool"]
    results = await get_pending_transactions(pool, account_id)
    if not results:
        return "No pending transactions — all transactions are settled."
    lines = [f"{len(results)} pending transaction(s):\n"]
    for t in results:
        lines.append(
            f"  [{t.id}] {t.created_at:%Y-%m-%d %H:%M} | {t.transaction_type:<12} | "
            f"${t.amount:>10} | {t.description}"
        )
    return "\n".join(lines)


# ---------------------------------------------------------------------------
# Scheduled Payments / Direct Debits
# ---------------------------------------------------------------------------

@mcp.tool
async def get_scheduled_payments_tool(
    account_id: int,
    include_inactive: bool = False,
) -> str:
    """Get all scheduled payments (direct debits) for an account.

    Args:
        account_id: The account ID.
        include_inactive: Whether to include cancelled/expired payments (default false).
    """
    pool = mcp.get_context().lifespan_context["pool"]
    results = await get_scheduled_payments(pool, account_id, include_inactive)
    if not results:
        return "No scheduled payments found for this account."
    lines = [f"{len(results)} scheduled payment(s):\n"]
    for p in results:
        status = "Active" if p.is_active else "Inactive"
        end = f" → {p.end_date}" if p.end_date else ""
        lines.append(
            f"  [{p.id}] {p.payee_name} | ${p.amount} {p.frequency} | "
            f"Next: {p.next_due_date} | {status} | Started: {p.start_date}{end}"
        )
        if p.description:
            lines.append(f"         Description: {p.description}")
    return "\n".join(lines)


@mcp.tool
async def get_payment_execution_history_tool(
    scheduled_payment_id: int,
    limit: int = 10,
) -> str:
    """Get recent execution history for a specific scheduled payment.
    Shows whether payments went through or were declined (insufficient funds).

    Args:
        scheduled_payment_id: The scheduled payment ID (from get_scheduled_payments).
        limit: Maximum results (default 10).
    """
    pool = mcp.get_context().lifespan_context["pool"]
    try:
        results = await get_payment_execution_history(pool, scheduled_payment_id, limit)
    except ValueError as e:
        return str(e)
    if not results:
        return "No execution history found for this scheduled payment."
    lines = [f"{len(results)} execution(s):\n"]
    for p in results:
        declined = " ⚠️ DECLINED" if p.was_declined else " ✓"
        lines.append(
            f"  [{p.transaction_id}] {p.created_at:%Y-%m-%d %H:%M} | "
            f"${p.amount} | {p.status}{declined} | {p.description}"
        )
    return "\n".join(lines)


# ---------------------------------------------------------------------------
# CRM / Notes
# ---------------------------------------------------------------------------

@mcp.tool
async def get_customer_notes_tool(
    customer_id: int,
    limit: int = 20,
) -> str:
    """Get previous interaction notes for a customer, written by staff.
    Provides context before speaking with the customer.

    Args:
        customer_id: The customer's ID.
        limit: Maximum notes to return (default 20).
    """
    pool = mcp.get_context().lifespan_context["pool"]
    results = await get_customer_notes(pool, customer_id, limit)
    if not results:
        return "No notes found for this customer."
    lines = [f"{len(results)} note(s):\n"]
    for n in results:
        lines.append(f"  [{n.created_at:%Y-%m-%d %H:%M}] {n.author}:")
        lines.append(f"    {n.content}")
        lines.append("")
    return "\n".join(lines)


# ---------------------------------------------------------------------------
# Analytics
# ---------------------------------------------------------------------------

@mcp.tool
async def get_spending_summary_tool(
    account_id: int,
    from_date: Optional[str] = None,
    to_date: Optional[str] = None,
) -> str:
    """Get a spending breakdown by transaction type for an account.

    Args:
        account_id: The account ID.
        from_date: Start date (YYYY-MM-DD). Defaults to 30 days ago.
        to_date: End date (YYYY-MM-DD). Defaults to today.
    """
    pool = mcp.get_context().lifespan_context["pool"]
    fd = _parse_date(from_date)
    td = _parse_date(to_date)
    s = await get_spending_summary(pool, account_id, fd, td)
    lines = [
        f"Spending summary for account {s.account_id} ({s.from_date} to {s.to_date}):\n",
        f"  Total spent:    ${s.total_spent}",
        f"  Total received: ${s.total_received}",
        f"\n  Breakdown by type:",
    ]
    for c in s.categories:
        lines.append(
            f"    {c.transaction_type:<14} ${c.total_amount:>12} ({c.transaction_count} transactions)"
        )
    return "\n".join(lines)


@mcp.tool
async def get_interest_summary_tool(
    account_id: int,
    from_date: Optional[str] = None,
    to_date: Optional[str] = None,
) -> str:
    """Get interest accrual summary for an account.
    Shows daily interest charges (for loans) or earnings (for savings).

    Args:
        account_id: The account ID.
        from_date: Start date (YYYY-MM-DD). Defaults to 30 days ago.
        to_date: End date (YYYY-MM-DD). Defaults to today.
    """
    pool = mcp.get_context().lifespan_context["pool"]
    fd = _parse_date(from_date)
    td = _parse_date(to_date)
    s = await get_interest_summary(pool, account_id, fd, td)
    lines = [
        f"Interest summary for account {s.account_id} ({s.from_date} to {s.to_date}):\n",
        f"  Total interest: ${s.total_interest} ({s.accrual_count} days)",
        f"\n  Daily accruals:",
    ]
    for a in s.accruals:
        posted = " [posted]" if a.posted else ""
        lines.append(f"    {a.accrual_date}: ${a.daily_amount}{posted}")
    return "\n".join(lines)


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
