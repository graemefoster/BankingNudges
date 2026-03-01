# Bank of Graeme 🏦

A simple Australian retail banking app — .NET 10 API, PostgreSQL, React/TypeScript frontend.

## Quick Start

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker](https://www.docker.com/) (for PostgreSQL)

### 1. Start the database

```bash
# One command to run everything:
./run.sh

# Or with a fresh seeded database:
./run.sh --fresh
```

This starts PostgreSQL, the API (http://localhost:5225), the customer UI (http://localhost:5173), and the staff CRM (http://localhost:5174). Press Ctrl+C to stop.

<details>
<summary>Manual startup (step by step)</summary>

```bash
docker compose up -d
```

### 2. Run the API

```bash
cd src/BankOfGraeme.Api
dotnet run
```

The API runs at **http://localhost:5225**. On first run it applies migrations and seeds sample data.

API docs: http://localhost:5225/openapi/v1.json

### 3. Run the frontend

```bash
cd src/bank-ui
npm install
npm run dev
```

The frontend runs at **http://localhost:5173** and proxies `/api` to the backend.

</details>

## Sample Customers

| Customer | Accounts |
|----------|----------|
| Sarah Mitchell | Transaction ($2,450), Savings ($15,000), Home Loan (-$450K), Offset ($35,000) |
| James Chen | Transaction ($890), Savings ($8,200) |
| Emma Wilson | Transaction ($5,100), Home Loan (-$320K), Offset ($12,000) |

## Account Types

- **Transaction** — everyday spending, no interest
- **Savings** — earns interest, limited withdrawals
- **Home Loan** — loan balance with interest charged
- **Offset** — linked to a home loan, balance reduces loan interest

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | .NET 10 Minimal API |
| ORM | Entity Framework Core + Npgsql |
| Database | PostgreSQL 16 |
| Frontend | React 19 + TypeScript + Vite |
| CRM Frontend | React 19 + TypeScript + Vite (separate app) |
| Styling | Tailwind CSS v4 |
| Routing | React Router |

## Color Palette

| Color | Hex | Usage |
|-------|-----|-------|
| Purple | `#CF2BE8` | Primary brand, Offset accounts |
| Mint Green | `#BAFFD3` | Success, Savings accounts |
| Yellow | `#FFE548` | Warnings, highlights |
| Blue | `#36A6E8` | Links, Transaction accounts |
| Orange-Red | `#FF5C3B` | Errors, Home Loan accounts |

## Staff CRM

The CRM is a separate internal app for bank staff at **http://localhost:5174**.

### Default Staff Credentials

| Username | Password | Role |
|----------|----------|------|
| admin | admin | Admin |
| teller | teller | Teller |

### CRM Features

- **Customer Management** — search, view, and edit customer details (name, email, phone, DOB)
- **Account Overview** — view all accounts per customer, including closed accounts
- **Balance Adjustments** — manual credits/debits with audit trail (reason required)
- **Account Closure** — close accounts (with force option for non-zero balances)
- **Customer Notes** — add and view timestamped notes with staff attribution
- **Transaction History** — paginated with filters (type, date, amount)

### CRM Color Palette

| Color | Hex | Usage |
|-------|-----|-------|
| Dark Teal | `#234D58` | Sidebar, headings |
| Bright Green | `#4DD588` | Primary actions, links |
| Muted Green | `#63A376` | Secondary elements |
| Light Green | `#AAD7B2` | Backgrounds, highlights |
| Orange/Coral | `#F07954` | Warnings, destructive actions |
