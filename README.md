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

This starts PostgreSQL, the API (http://localhost:5225), the customer UI (http://localhost:5173), the staff CRM (http://localhost:5174), and the Azure Functions nightly batch (http://localhost:7071). Press Ctrl+C to stop.

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
| Nightly Batch | Azure Functions (isolated worker) |
| Frontend | React 19 + TypeScript + Vite |
| CRM Frontend | React 19 + TypeScript + Vite (separate app) |
| Styling | Tailwind CSS v4 |
| Routing | React Router |

## Virtual Time (IDateTimeProvider)

All time in the application is virtualised via `IDateTimeProvider`. **Never use `DateTime.UtcNow` or `DateTimeOffset.UtcNow` directly.** Instead, inject `IDateTimeProvider` and use its `UtcNow` or `Today` properties.

### How it works

- `SystemSettings` table stores a `DaysAdvanced` key (integer offset)
- `DatabaseDateTimeProvider` reads this on first access and returns `DateTime.UtcNow.AddDays(offset)`
- `BankDbContext.SaveChanges` automatically stamps `CreatedAt` on new entities with virtual time
- Both UIs show a yellow banner when time has been advanced

### Time Travel API

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/time-travel/current` | GET | Returns current virtual date and offset |
| `/api/time-travel/advance` | POST | Advance by `{ "days": N }` — runs interest batch for each intermediate day |
| `/api/time-travel/reset` | POST | Reset virtual time to real time |

### Convention for contributors

When adding new code that needs the current time:
1. Inject `IDateTimeProvider` via constructor
2. Use `dateTime.UtcNow` instead of `DateTime.UtcNow`
3. For `CreatedAt` on entities — do nothing, the `SaveChanges` interceptor handles it automatically

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
