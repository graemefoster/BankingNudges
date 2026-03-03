# Bank of Graeme — MCP Server

An MCP (Model Context Protocol) server that exposes Bank of Graeme customer and account data to AI agents. Designed for use by a contact centre where agents need to quickly look up customers, investigate transactions, and answer account queries.

## Quick Start

```bash
# From the bank-mcp directory
cd src/bank-mcp

# Install dependencies
pip install -e .

# Ensure PostgreSQL is running (via docker compose from the repo root)
docker compose up -d

# Run the MCP server (HTTP transport on port 8080)
bank-mcp

# Or run with STDIO transport (for Claude Desktop / local tools)
bank-mcp --stdio
```

## Configuration

| Environment Variable | Default | Description |
|---|---|---|
| `DATABASE_URL` | `postgresql://bankadmin:BankDev123!@localhost:5432/bankofgraeme` | PostgreSQL connection string |

## Available Tools (14)

### 🔍 Customer Lookup
| Tool | Description |
|---|---|
| `search_customers_tool` | Search by name, email, phone, or account number |
| `get_customer_profile_tool` | Full profile with all accounts and net position |

### 💰 Account Details
| Tool | Description |
|---|---|
| `get_account_details_tool` | Balance, available balance, loan info, offset accounts |
| `get_balance_history_tool` | End-of-day balance snapshots over a date range |

### 📋 Transaction Investigation
| Tool | Description |
|---|---|
| `get_transactions_tool` | Filtered, paginated transaction list |
| `search_transactions_tool` | Search across all customer accounts by description |
| `find_similar_transactions_tool` | Find similar transactions for dispute investigation |
| `find_potential_duplicate_charges_tool` | Detect potential duplicate charges |
| `get_pending_transactions_tool` | Show unsettled transactions |

### 📅 Scheduled Payments
| Tool | Description |
|---|---|
| `get_scheduled_payments_tool` | View direct debits and scheduled payments |
| `get_payment_execution_history_tool` | Check if payments went through or were declined |

### 📝 CRM Notes
| Tool | Description |
|---|---|
| `get_customer_notes_tool` | Previous interaction notes from staff |

### 📊 Analytics
| Tool | Description |
|---|---|
| `get_spending_summary_tool` | Spending breakdown by transaction type |
| `get_interest_summary_tool` | Interest accrual details for loans/savings |

## Architecture

- **Transport**: Streamable HTTP (default, port 8080) or STDIO
- **Database**: Direct read-only PostgreSQL connection via `asyncpg`
- **Framework**: [FastMCP](https://gofastmcp.dev)
- **No authentication** — runs within the trusted local network alongside the API

## Contact Centre Use Cases

1. **"I don't recognise this charge"** → `search_transactions_tool` → `find_similar_transactions_tool` → `find_potential_duplicate_charges_tool`
2. **"My balance seems wrong"** → `get_account_details_tool` (available vs ledger) → `get_pending_transactions_tool` → `get_balance_history_tool`
3. **"My direct debit didn't go through"** → `get_scheduled_payments_tool` → `get_payment_execution_history_tool`
4. **"How much interest am I paying?"** → `get_interest_summary_tool` → `get_account_details_tool` (rate + offset info)
5. **"I'm spending too much"** → `get_spending_summary_tool`
