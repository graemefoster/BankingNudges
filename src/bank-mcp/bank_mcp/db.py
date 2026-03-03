from __future__ import annotations

import os
from contextlib import asynccontextmanager
from typing import Any

import asyncpg

DATABASE_URL = os.environ.get(
    "DATABASE_URL",
    "postgresql://bankadmin:BankDev123!@localhost:5432/bankofgraeme",
)


async def create_pool() -> asyncpg.Pool:
    return await asyncpg.create_pool(
        DATABASE_URL,
        min_size=2,
        max_size=10,
        command_timeout=30,
    )


async def fetch_all(
    pool: asyncpg.Pool, query: str, *args: Any
) -> list[dict[str, Any]]:
    async with pool.acquire() as conn:
        rows = await conn.fetch(query, *args)
        return [dict(r) for r in rows]


async def fetch_one(
    pool: asyncpg.Pool, query: str, *args: Any
) -> dict[str, Any] | None:
    async with pool.acquire() as conn:
        row = await conn.fetchrow(query, *args)
        return dict(row) if row else None
