from __future__ import annotations

from datetime import date, datetime, timedelta
from decimal import Decimal

import asyncpg

from bank_mcp.db import fetch_all, fetch_one
from bank_mcp.models import (
    DuplicateGroup,
    SimilarTransaction,
    TransactionPage,
    TransactionRecord,
    TRANSACTION_TYPE_MAP,
)

# Reverse lookup: "Withdrawal" -> 1, etc.
_TYPE_NAME_TO_INT: dict[str, int] = {v: k for k, v in TRANSACTION_TYPE_MAP.items()}


def _map_transaction(row: dict) -> TransactionRecord:
    return TransactionRecord(
        id=row["Id"],
        account_id=row["AccountId"],
        amount=str(row["Amount"]),
        description=row["Description"],
        transaction_type=TRANSACTION_TYPE_MAP.get(row["TransactionType"], "Unknown"),
        status=row["Status"],
        settled_at=row["SettledAt"],
        created_at=row["CreatedAt"],
    )


# ---------------------------------------------------------------------------
# 1. get_transactions – paginated, filtered transaction list
# ---------------------------------------------------------------------------

async def get_transactions(
    pool: asyncpg.Pool,
    account_id: int,
    from_date: date | None = None,
    to_date: date | None = None,
    transaction_type: str | None = None,
    min_amount: float | None = None,
    max_amount: float | None = None,
    description: str | None = None,
    status: str | None = None,
    page: int = 1,
    page_size: int = 20,
) -> TransactionPage:
    conditions: list[str] = ['"AccountId" = $1']
    params: list[object] = [account_id]
    idx = 2

    # Exclude reversed unless explicitly requested
    if status:
        conditions.append(f'"Status" = ${idx}')
        params.append(status)
        idx += 1
    else:
        conditions.append(""""Status" != 'Reversed'""")

    if from_date:
        conditions.append(f'"CreatedAt" >= ${idx}')
        params.append(datetime(from_date.year, from_date.month, from_date.day))
        idx += 1

    if to_date:
        conditions.append(f'"CreatedAt" < ${idx}')
        params.append(
            datetime(to_date.year, to_date.month, to_date.day) + timedelta(days=1)
        )
        idx += 1

    if transaction_type:
        type_int = _TYPE_NAME_TO_INT.get(transaction_type)
        if type_int is not None:
            conditions.append(f'"TransactionType" = ${idx}')
            params.append(type_int)
            idx += 1

    if min_amount is not None:
        conditions.append(f'ABS("Amount") >= ${idx}')
        params.append(Decimal(str(min_amount)))
        idx += 1

    if max_amount is not None:
        conditions.append(f'ABS("Amount") <= ${idx}')
        params.append(Decimal(str(max_amount)))
        idx += 1

    if description:
        conditions.append(f'"Description" ILIKE ${idx}')
        params.append(f"%{description}%")
        idx += 1

    where = " AND ".join(conditions)

    count_query = f'SELECT COUNT(*) AS cnt FROM "Transactions" WHERE {where}'
    total_row = await fetch_one(pool, count_query, *params)
    total = total_row["cnt"] if total_row else 0

    offset = (page - 1) * page_size
    data_query = (
        f'SELECT * FROM "Transactions" WHERE {where} '
        f'ORDER BY "CreatedAt" DESC LIMIT ${idx} OFFSET ${idx + 1}'
    )
    params.extend([page_size, offset])

    rows = await fetch_all(pool, data_query, *params)
    transactions = [_map_transaction(r) for r in rows]

    return TransactionPage(
        transactions=transactions,
        total=total,
        page=page,
        page_size=page_size,
    )


# ---------------------------------------------------------------------------
# 2. search_transactions – cross-account description search
# ---------------------------------------------------------------------------

async def search_transactions(
    pool: asyncpg.Pool,
    customer_id: int,
    description_query: str,
    from_date: date | None = None,
    to_date: date | None = None,
    limit: int = 20,
) -> list[TransactionRecord]:
    conditions: list[str] = [
        '"Accounts"."CustomerId" = $1',
        '"Transactions"."Description" ILIKE $2',
    ]
    params: list[object] = [customer_id, f"%{description_query}%"]
    idx = 3

    if from_date:
        conditions.append(f'"Transactions"."CreatedAt" >= ${idx}')
        params.append(datetime(from_date.year, from_date.month, from_date.day))
        idx += 1

    if to_date:
        conditions.append(f'"Transactions"."CreatedAt" < ${idx}')
        params.append(
            datetime(to_date.year, to_date.month, to_date.day) + timedelta(days=1)
        )
        idx += 1

    where = " AND ".join(conditions)
    query = (
        f'SELECT "Transactions".* FROM "Transactions" '
        f'INNER JOIN "Accounts" ON "Transactions"."AccountId" = "Accounts"."Id" '
        f"WHERE {where} "
        f'ORDER BY "Transactions"."CreatedAt" DESC '
        f"LIMIT ${idx}"
    )
    params.append(limit)

    rows = await fetch_all(pool, query, *params)
    return [_map_transaction(r) for r in rows]


# ---------------------------------------------------------------------------
# 3. find_similar_transactions – dispute investigation helper
# ---------------------------------------------------------------------------

def _significant_words(description: str, count: int = 2) -> list[str]:
    """Extract the first *count* significant words from a description."""
    stop = {"the", "a", "an", "to", "for", "of", "in", "on", "at", "and", "or", "is"}
    words = [
        w for w in description.split() if w.lower() not in stop and len(w) > 1
    ]
    return words[:count]


async def find_similar_transactions(
    pool: asyncpg.Pool,
    transaction_id: int,
    similarity_window_days: int = 90,
    amount_tolerance_pct: float = 10.0,
) -> list[SimilarTransaction]:
    # Fetch the source transaction + customer info
    source = await fetch_one(
        pool,
        'SELECT t.*, a."CustomerId", a."Name" AS "AccountName" '
        'FROM "Transactions" t '
        'INNER JOIN "Accounts" a ON t."AccountId" = a."Id" '
        'WHERE t."Id" = $1',
        transaction_id,
    )
    if source is None:
        raise ValueError(f"Transaction {transaction_id} not found")

    src_amount = abs(source["Amount"])
    tolerance = src_amount * Decimal(str(amount_tolerance_pct)) / Decimal("100")
    lo = src_amount - tolerance
    hi = src_amount + tolerance

    window_start = source["CreatedAt"] - timedelta(days=similarity_window_days)
    window_end = source["CreatedAt"] + timedelta(days=similarity_window_days)

    # Build description ILIKE conditions from significant words
    sig_words = _significant_words(source["Description"])
    desc_likes: list[str] = []
    params: list[object] = [
        source["CustomerId"],
        transaction_id,
        window_start,
        window_end,
        lo,
        hi,
    ]
    idx = 7
    for word in sig_words:
        desc_likes.append(f't."Description" ILIKE ${idx}')
        params.append(f"%{word}%")
        idx += 1

    desc_condition = " AND ".join(desc_likes) if desc_likes else "FALSE"

    query = (
        'SELECT t.*, a."Name" AS "AccountName" '
        'FROM "Transactions" t '
        'INNER JOIN "Accounts" a ON t."AccountId" = a."Id" '
        'WHERE a."CustomerId" = $1 '
        '  AND t."Id" != $2 '
        '  AND t."CreatedAt" BETWEEN $3 AND $4 '
        """  AND t."Status" != 'Reversed' """
        f"  AND (({desc_condition}) OR (ABS(t.\"Amount\") BETWEEN $5 AND $6)) "
        'ORDER BY t."CreatedAt" DESC'
    )

    rows = await fetch_all(pool, query, *params)

    results: list[SimilarTransaction] = []
    for row in rows:
        desc_match = all(
            w.lower() in row["Description"].lower() for w in sig_words
        ) if sig_words else False
        amt_match = lo <= abs(row["Amount"]) <= hi

        if desc_match and amt_match:
            reason = "similar description and amount"
        elif desc_match:
            reason = "similar description"
        else:
            reason = "similar amount"

        results.append(
            SimilarTransaction(
                id=row["Id"],
                account_id=row["AccountId"],
                account_name=row["AccountName"],
                amount=str(row["Amount"]),
                description=row["Description"],
                transaction_type=TRANSACTION_TYPE_MAP.get(
                    row["TransactionType"], "Unknown"
                ),
                status=row["Status"],
                created_at=row["CreatedAt"],
                similarity_reason=reason,
            )
        )
    return results


# ---------------------------------------------------------------------------
# 4. find_potential_duplicate_charges – duplicate detection
# ---------------------------------------------------------------------------

async def find_potential_duplicate_charges(
    pool: asyncpg.Pool,
    account_id: int,
    lookback_days: int = 30,
    time_window_days: int = 3,
) -> list[DuplicateGroup]:
    cutoff = datetime.utcnow() - timedelta(days=lookback_days)

    query = (
        'SELECT * FROM "Transactions" '
        'WHERE "AccountId" = $1 '
        '  AND "Amount" < 0 '
        """  AND "Status" != 'Reversed' """
        '  AND "CreatedAt" >= $2 '
        'ORDER BY "CreatedAt" DESC'
    )
    rows = await fetch_all(pool, query, account_id, cutoff)

    # Group by (absolute amount, first 10 chars of description lowered)
    from collections import defaultdict

    buckets: dict[tuple[Decimal, str], list[dict]] = defaultdict(list)
    for row in rows:
        key = (abs(row["Amount"]), row["Description"][:10].lower())
        buckets[key].append(row)

    groups: list[DuplicateGroup] = []
    for (amount, desc_prefix), txns in buckets.items():
        if len(txns) < 2:
            continue

        # Check if any pair falls within the time window
        txns_sorted = sorted(txns, key=lambda r: r["CreatedAt"])
        cluster: list[dict] = []
        for i, txn in enumerate(txns_sorted):
            for j in range(i + 1, len(txns_sorted)):
                diff = abs(
                    (txns_sorted[j]["CreatedAt"] - txn["CreatedAt"]).total_seconds()
                )
                if diff <= time_window_days * 86400:
                    if txn not in cluster:
                        cluster.append(txn)
                    if txns_sorted[j] not in cluster:
                        cluster.append(txns_sorted[j])

        if len(cluster) >= 2:
            cluster.sort(key=lambda r: r["CreatedAt"], reverse=True)
            groups.append(
                DuplicateGroup(
                    amount=str(amount),
                    description_pattern=f"{desc_prefix}%",
                    transactions=[_map_transaction(r) for r in cluster],
                )
            )

    return groups


# ---------------------------------------------------------------------------
# 5. get_pending_transactions – unsettled transactions
# ---------------------------------------------------------------------------

async def get_pending_transactions(
    pool: asyncpg.Pool,
    account_id: int,
) -> list[TransactionRecord]:
    query = (
        'SELECT * FROM "Transactions" '
        """WHERE "AccountId" = $1 AND "Status" = 'Pending' """
        'ORDER BY "CreatedAt" DESC'
    )
    rows = await fetch_all(pool, query, account_id)
    return [_map_transaction(r) for r in rows]
