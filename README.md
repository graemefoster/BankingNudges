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

## Seed Data Highlights

The seeder creates **150 customers** across 8 personas with realistic Australian spending. Key features:

- **Recurring payments** — rent, utilities, mobile, insurance with scheduled direct debits
- **Holiday travel** — some customers book overseas trips (Bali, Thailand, Japan, Europe, etc.) and spend in foreign currencies with exchange rate conversion and 3% international transaction fees. Domestic discretionary spending is suppressed while abroad; recurring bills still fire. See [customer-personas.md](customer-personas.md#holiday-travel) for full details.
- **Failed transactions** — insufficient-funds failures on direct debits
- **Savings with bonus interest** — 5% p.a. (0.50% base + 4.50% conditional bonus)

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
| AI Nudges | Azure OpenAI (Responses API) via openai-dotnet SDK |
| AI Chat Agent | Microsoft Agent Framework (`Microsoft.Agents.AI.OpenAI`) |
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

## Proactive Nudges (AI)

The nudge system uses Azure OpenAI to analyse customer financial data and generate personalised nudges before customers encounter problems.

### Architecture

```
Pattern Detector  → detects recurring payments from transaction history
Signal Detector   → rules-based flags (low balance, spend spike, upcoming payment)
Context Assembler → builds rich financial snapshot per customer
Nudge Generator   → calls Azure OpenAI Responses API, validates output
Batch Runner      → orchestrates per-customer, sequential processing
Nightly Service   → expires stale nudges + generates fresh ones during rollover
```

### Design Decisions

| Decision | Rationale |
|----------|-----------|
| **PENDING nudges expire after 3 days** | A nudge saying "rent due in 2 days" becomes misleading after the payment date. Context changes daily, so stale nudges risk being inaccurate. Expired nudges are marked EXPIRED (not deleted) for audit. |
| **Signals are not persisted** | Signals are derived from current state (balance, transactions, upcoming). Storing them would create stale data. The `context_snapshot` on each nudge captures signals at generation time for audit. |
| **Max 3 nudges per customer per 7-day window** | Prevents notification fatigue. Checked before calling the LLM. |
| **Hallucination check on all $ amounts** | Every dollar figure in the LLM response is verified against the context data. Nudges with invented numbers are rejected. |
| **Nudge processing runs via API, not nightly rollover** | LLM calls are slow and external — they don't belong in the financial processing pipeline. Use `POST /api/nudges/batch/run` to generate nudges on-demand or via a separate schedule. |

### Configuration

| Setting | Description |
|---------|-------------|
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI resource endpoint (required) |
| `AZURE_OPENAI_DEPLOYMENT` | Model deployment name (default: `gpt-4o`) |

Authentication uses `DefaultAzureCredential` — no API key needed when running with Azure CLI or Managed Identity.

### Nudge API

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/nudges/{customerId}` | GET | Latest PENDING nudge for a customer |
| `/api/nudges/{nudgeId}/respond` | POST | Respond: `ACCEPTED`, `DISMISSED`, `SNOOZED` |
| `/api/nudges/batch/run` | POST | Trigger batch `{ sampleSize?, customerIds? }` |
| `/api/nudges/{customerId}/history` | GET | Last 10 nudges with outcomes |
| `/api/customers/{customerId}/context` | GET | Raw context sent to LLM (debug) |

## Nudge Chat (Agentic)

Customers can chat with an AI agent about their nudges via a slide-out drawer on the dashboard. The agent is pre-seeded with the nudge's financial context snapshot and can fetch nudge history on demand.

### Architecture

Built with **Microsoft Agent Framework** (`Microsoft.Agents.AI.OpenAI`) — the successor to Semantic Kernel for agentic patterns. The agent uses `AsAIAgent()` with the existing Azure OpenAI deployment.

```
NudgeChatAgent    → builds system prompt from nudge + ContextSnapshot
NudgeChatTools    → tool: GetNudgeHistory (on-demand, agent-callable)
ChatEndpoints     → SSE streaming endpoint for real-time responses
ChatDrawer (React)→ slide-up drawer with streaming message display
```

### Context Strategy (Hybrid)

- **Pre-seeded**: System prompt contains the current nudge message, reasoning, full financial summary (accounts, spend by category, deltas, upcoming payments, active signals)
- **Tool**: `GetNudgeHistory` is available for the agent to call when the customer asks about previous insights

### Chat API

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/chat/nudge/start` | POST | Start session: `{ customerId, nudgeId }` → `{ sessionId }` |
| `/api/chat/nudge/message` | POST | Send message: `{ sessionId, message }` → SSE stream |
| `/api/chat/nudge/{sessionId}` | DELETE | Clean up session |

### Design Decisions

| Decision | Rationale |
|----------|-----------|
| **Microsoft Agent Framework, not Semantic Kernel** | Agent Framework (`Microsoft.Agents.AI`) is Microsoft's successor, using `AsAIAgent()` + `RunStreamingAsync()` pattern |
| **Hybrid context (pre-seed + tool)** | Pre-seeding avoids round-trips for common queries; tool avoids bloating every prompt with full history |
| **Per-request agent creation** | Each chat session creates a fresh agent with the specific nudge's context. No shared state between customers |
| **SSE streaming** | Tokens stream to the frontend as they're generated for a responsive chat feel |
| **Sessions are ephemeral** | Chat history lives in-memory during the session. Future: persist to DB for audit |
