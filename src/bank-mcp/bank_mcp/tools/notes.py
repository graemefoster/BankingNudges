import asyncpg

from bank_mcp.db import fetch_all
from bank_mcp.models import CustomerNote


async def get_customer_notes(
    pool: asyncpg.Pool, customer_id: int, limit: int = 20
) -> list[CustomerNote]:
    """Get previous interaction notes for a customer."""
    rows = await fetch_all(
        pool,
        """
        SELECT cn."Id", cn."Content", su."DisplayName" AS "Author", cn."CreatedAt"
        FROM "CustomerNotes" cn
        JOIN "StaffUsers" su ON su."Id" = cn."StaffUserId"
        WHERE cn."CustomerId" = $1
        ORDER BY cn."CreatedAt" DESC
        LIMIT $2
        """,
        customer_id,
        limit,
    )
    return [
        CustomerNote(
            id=r["Id"],
            content=r["Content"],
            author=r["Author"],
            created_at=r["CreatedAt"],
        )
        for r in rows
    ]
