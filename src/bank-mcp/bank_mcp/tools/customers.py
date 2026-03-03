from __future__ import annotations

import asyncpg

from bank_mcp.db import fetch_all, fetch_one
from bank_mcp.models import CustomerSummary, CustomerProfile, AccountSummary, ACCOUNT_TYPE_MAP


async def search_customers(
    pool: asyncpg.Pool, query: str, limit: int = 10
) -> list[CustomerSummary]:
    """Search customers by name, email, phone, or account number."""
    pattern = f"%{query}%"
    sql = """
        SELECT DISTINCT
            c."Id",
            c."FirstName",
            c."LastName",
            c."Email",
            c."Phone",
            c."DateOfBirth"
        FROM "Customers" c
        LEFT JOIN "Accounts" a ON a."CustomerId" = c."Id"
        WHERE c."FirstName" ILIKE $1
           OR c."LastName" ILIKE $1
           OR c."Email" ILIKE $1
           OR c."Phone" ILIKE $1
           OR a."AccountNumber" ILIKE $1
           OR (a."Bsb" || a."AccountNumber") ILIKE $1
        ORDER BY c."LastName", c."FirstName"
        LIMIT $2
    """
    rows = await fetch_all(pool, sql, pattern, limit)
    return [
        CustomerSummary(
            id=r["Id"],
            first_name=r["FirstName"],
            last_name=r["LastName"],
            email=r["Email"],
            phone=r["Phone"],
            date_of_birth=r["DateOfBirth"],
        )
        for r in rows
    ]


async def get_customer_profile(
    pool: asyncpg.Pool, customer_id: int
) -> CustomerProfile:
    """Get full customer profile with all accounts and net position."""
    customer = await fetch_one(
        pool,
        """
        SELECT "Id", "FirstName", "LastName", "Email", "Phone",
               "DateOfBirth", "CreatedAt"
        FROM "Customers"
        WHERE "Id" = $1
        """,
        customer_id,
    )
    if customer is None:
        raise ValueError(f"Customer {customer_id} not found")

    rows = await fetch_all(
        pool,
        """
        SELECT "Id", "AccountType", "Bsb", "AccountNumber",
               "Name", "Balance", "IsActive"
        FROM "Accounts"
        WHERE "CustomerId" = $1
        ORDER BY "Id"
        """,
        customer_id,
    )

    accounts = [
        AccountSummary(
            id=r["Id"],
            account_type=ACCOUNT_TYPE_MAP.get(r["AccountType"], "Unknown"),
            bsb=r["Bsb"],
            account_number=r["AccountNumber"],
            name=r["Name"],
            balance=f"{r['Balance']:.2f}",
            is_active=r["IsActive"],
        )
        for r in rows
    ]

    net_position = sum(r["Balance"] for r in rows)

    return CustomerProfile(
        id=customer["Id"],
        first_name=customer["FirstName"],
        last_name=customer["LastName"],
        email=customer["Email"],
        phone=customer["Phone"],
        date_of_birth=customer["DateOfBirth"],
        created_at=customer["CreatedAt"],
        accounts=accounts,
        net_position=f"{net_position:.2f}",
    )
