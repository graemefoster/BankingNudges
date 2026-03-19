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
| **Branches & ATMs have geolocations** | Branch and ATM entities carry real Australian lat/lng coordinates. Transactions link to them via optional FKs (`BranchId`, `AtmId`) — designed for future geo-spatial queries. Each seeded customer is assigned a "home branch" (`customerId % branchCount`) so ATM/branch usage clusters geographically |

## Neo4j Graph Snapshot

A separate .NET console app (`BankOfGraeme.Neo4j`) builds a point-in-time graph snapshot of banking data in Neo4j. This enables graph-based analysis of customer relationships, branch usage patterns, and transaction flows.

### Running the graph sync

```bash
# Start all infrastructure (including Neo4j)
docker compose up -d

# Run the graph sync tool
cd src/BankOfGraeme.Neo4j
dotnet run
```

### Neo4j Browser

Open **http://localhost:7474** (credentials: `neo4j` / `neo4jdev123`) to explore the graph visually.

### Graph Model

| Node | Relationships |
|------|--------------|
| `Customer` | `-[:OWNS]->` Account |
| `Account` | `-[:RECORDED]->` Transaction, `-[:OFFSETS]->` Account (offset→loan) |
| `Transaction` | `-[:AT_BRANCH]->` Branch, `-[:AT_ATM]->` Atm |
| `Atm` | `-[:LOCATED_AT]->` Branch |

### Example Cypher Queries

```cypher
-- Which branches does each customer use most?
MATCH (c:Customer)-[:OWNS]->()-[:RECORDED]->(t)-[:AT_BRANCH]->(b:Branch)
RETURN c.firstName + ' ' + c.lastName AS customer, b.name AS branch, count(t) AS visits
ORDER BY visits DESC LIMIT 20

-- ATM usage by persona
MATCH (c:Customer)-[:OWNS]->()-[:RECORDED]->(t)-[:AT_ATM]->(a:Atm)
RETURN c.persona, a.locationName, count(t) AS uses
ORDER BY uses DESC

-- Customer account graph (good starting point for visual exploration)
MATCH (c:Customer)-[:OWNS]->(a:Account)
RETURN c, a LIMIT 50
```

### Design Notes

- **Snapshot, not real-time**: The sync clears and reloads the full graph. Run it whenever you want a fresh snapshot. Real-time CDC can be added later.
- **Idempotent**: Safe to run multiple times — always produces the same result.
- **Batch loading**: Uses Cypher `UNWIND` for bulk operations (~1000 items per batch) for good performance even with 150k+ transactions.

## APP Fraud Detection — "Operation Swift"

The seed data includes an **Authorised Push Payment (APP) fraud scenario** — a scam network with 4 mule accounts and 5 victims. The graph reveals the hidden money flow that's invisible in flat SQL tables.

### The Scam Network

| Mule | Role | Account Profile |
|------|------|-----------------|
| **Marcus Webb** | Collector | 🔥 Burner — 3 weeks old, almost no normal activity |
| **Jade Thornton** | Layer 1 | 🏠 Established — 8 months old, salary/rent/groceries |
| **Ryan Kovac** | Layer 2 | 🔥 Burner — 2 weeks old, minimal activity |
| **Priya Desai** | Cash-out | 🏠 Semi-established — 4 months old, some normal activity |

Victims (Gabriel White, Margaret Kelly, Chloe Martin, Grace Turner, Noah Patel) are socially engineered into making payments to Marcus. The money cascades through the network with commission taken at each hop (~8% → ~5% → ~3%). Each hop is a **completely separate, unlinked payment** — no transaction IDs or foreign keys connect them.

### Why Neo4j?

Individual transactions are independent records. A relational database would need expensive recursive CTEs with multiple self-joins across millions of rows to follow the trail. Neo4j traverses the graph naturally — matching credit/debit pairs across accounts by timestamp and amount, following the chain hop-by-hop.

### Fraud Detection Cypher Queries

> **Prerequisite**: Run the graph sync after seeding: `cd src/BankOfGraeme.Neo4j && dotnet run`

**1. Reconstruct money flow** — Find cross-customer transfers by matching debit/credit pairs that share a timestamp (natural property of same-bank payments):

```cypher
// Both legs of a bank payment share the same createdAt.
// This reconstructs "who sent money to whom" without any explicit links.
MATCH (c1:Customer)-[:OWNS]->(sender:Account)-[:RECORDED]->(debit:Transaction),
      (c2:Customer)-[:OWNS]->(receiver:Account)-[:RECORDED]->(credit:Transaction)
WHERE toFloat(debit.amount) < 0
  AND toFloat(credit.amount) > 0
  AND debit.createdAt = credit.createdAt
  AND abs(toFloat(debit.amount)) = toFloat(credit.amount)
  AND sender <> receiver
  AND c1 <> c2
  AND debit.transactionType = 'Transfer'
RETURN c1.firstName + ' ' + c1.lastName AS sender,
       c2.firstName + ' ' + c2.lastName AS receiver,
       toFloat(credit.amount) AS amount,
       debit.createdAt AS timestamp
ORDER BY timestamp
```

**2. Fan-in detector** — Accounts receiving from 3+ unique senders (the collector pattern):

```cypher
MATCH (sender:Customer)-[:OWNS]->(sAcct:Account)-[:RECORDED]->(debit:Transaction),
      (receiver:Customer)-[:OWNS]->(rAcct:Account)-[:RECORDED]->(credit:Transaction)
WHERE toFloat(debit.amount) < 0
  AND toFloat(credit.amount) > 0
  AND debit.createdAt = credit.createdAt
  AND abs(toFloat(debit.amount)) = toFloat(credit.amount)
  AND sAcct <> rAcct AND sender <> receiver
  AND debit.transactionType = 'Transfer'
WITH receiver, rAcct, collect(DISTINCT sender) AS senders,
     collect(toFloat(credit.amount)) AS amounts
WHERE size(senders) >= 3
RETURN receiver.firstName + ' ' + receiver.lastName AS suspiciousReceiver,
       rAcct.accountNumber AS account,
       size(senders) AS uniqueSenders,
       [s IN senders | s.firstName + ' ' + s.lastName] AS senderNames,
       reduce(total = 0.0, a IN amounts | total + a) AS totalReceived
ORDER BY uniqueSenders DESC
```

**3. Rapid transit detector** — Accounts where money arrives and departs the same day (passthrough pattern):

```cypher
MATCH (rAcct:Account)-[:RECORDED]->(credit:Transaction)
WHERE toFloat(credit.amount) > 0 AND credit.transactionType = 'Transfer'
WITH rAcct, credit
MATCH (rAcct)-[:RECORDED]->(debit:Transaction)
WHERE toFloat(debit.amount) < 0
  AND debit.transactionType = 'Transfer'
  AND debit.createdAt > credit.createdAt
  AND datetime(debit.createdAt) <= datetime(credit.createdAt) + duration('PT24H')
WITH rAcct, credit, debit,
     toFloat(credit.amount) AS inAmount,
     abs(toFloat(debit.amount)) AS outAmount
WHERE outAmount >= inAmount * 0.85 AND outAmount <= inAmount * 0.99
MATCH (owner:Customer)-[:OWNS]->(rAcct)
RETURN owner.firstName + ' ' + owner.lastName AS passthroughAccount,
       rAcct.accountNumber AS account,
       inAmount, outAmount,
       round((1.0 - outAmount / inAmount) * 100, 1) AS commissionPct,
       credit.createdAt AS moneyIn,
       debit.createdAt AS moneyOut
ORDER BY moneyIn
```

**4. Scam chain tracer** — Follow reported scam proceeds across an unknown number of mule hops:

```cypher
// Step 1: derive synthetic TRANSFER_TO edges between accounts.
// This is not source-of-truth banking data - it is an analysis shortcut
// inferred from matching transfer debit/credit pairs.
MATCH (src:Account)-[:RECORDED]->(debit:Transaction),
      (dst:Account)-[:RECORDED]->(credit:Transaction)
WHERE src <> dst
  AND debit.transactionType = 'Transfer'
  AND credit.transactionType = 'Transfer'
  AND debit.status = 'Settled'
  AND credit.status = 'Settled'
  AND toFloat(debit.amount) < 0
  AND toFloat(credit.amount) > 0
  AND datetime(credit.createdAt) >= datetime(debit.createdAt) - duration('PT5M')
  AND datetime(credit.createdAt) <= datetime(debit.createdAt) + duration('PT5M')
  AND abs(abs(toFloat(debit.amount)) - toFloat(credit.amount)) <= 0.01
MERGE (src)-[r:TRANSFER_TO {debitTxnId: debit.id, creditTxnId: credit.id}]->(dst)
SET r.sentAt     = debit.createdAt,
    r.outAmount  = abs(toFloat(debit.amount)),
    r.inAmount   = toFloat(credit.amount),
    r.lossAmount = abs(toFloat(debit.amount)) - toFloat(credit.amount),
    r.lossPct    = CASE
                     WHEN abs(toFloat(debit.amount)) = 0 THEN 0.0
                     ELSE 1.0 - (toFloat(credit.amount) / abs(toFloat(debit.amount)))
                   END;

// Step 2: trace a reported scam payment through the chain.
// Replace these seed values with the customer, amount, and rough timestamp
// from the case you are investigating. LIMIT 1 returns the most complete
// candidate chain first; widen or remove it if you want alternative paths.
WITH
  2 AS customerId, // Noah Patel in the seeded scam scenario
  1200.00 AS claimedAmount,
  datetime('2026-03-16T10:20:00Z') AS approxAt,
  duration('PT90M') AS firstHopWindow,
  50.00 AS firstHopAmountTolerance,
  duration('PT24H') AS maxGapPerHop,
  0.70 AS minRetentionPct,
  1.00 AS maxRetentionPct
MATCH (victim:Customer {id: customerId})-[:OWNS]->(start:Account)-[first:TRANSFER_TO]->(a1:Account)
WHERE datetime(first.sentAt) >= approxAt - firstHopWindow
  AND datetime(first.sentAt) <= approxAt + firstHopWindow
  AND abs(first.outAmount - claimedAmount) <= firstHopAmountTolerance
MATCH p = (a1)-[rest:TRANSFER_TO*0..6]->(:Account)
WHERE all(n IN ([start] + nodes(p)) WHERE single(m IN ([start] + nodes(p)) WHERE m = n))
WITH victim, start, [first] + rest AS hops, [start] + nodes(p) AS accounts
WHERE all(i IN range(1, size(hops) - 1) WHERE
    datetime(hops[i].sentAt) >= datetime(hops[i - 1].sentAt) AND
    datetime(hops[i].sentAt) <= datetime(hops[i - 1].sentAt) + maxGapPerHop AND
    hops[i].outAmount <= hops[i - 1].inAmount * maxRetentionPct AND
    hops[i].outAmount >= hops[i - 1].inAmount * minRetentionPct
)
UNWIND range(0, size(accounts) - 1) AS idx
WITH victim, start, hops, accounts, idx, accounts[idx] AS acct
OPTIONAL MATCH (owner:Customer)-[:OWNS]->(acct)
WITH victim, start, hops, accounts, idx, owner
ORDER BY idx
WITH victim, start, hops, accounts,
     collect(coalesce(owner.firstName + ' ' + owner.lastName, 'unknown')) AS owners
RETURN victim.firstName + ' ' + victim.lastName AS victim,
       start.accountNumber AS sourceAccount,
       size(hops) AS hopCount,
       round(hops[0].outAmount * 100) / 100.0 AS reportedLoss,
       round(hops[size(hops) - 1].inAmount * 100) / 100.0 AS tracedAmount,
       round((hops[size(hops) - 1].inAmount / hops[0].outAmount) * 1000) / 10.0 AS retainedPct,
       [i IN range(0, size(hops) - 1) | {
         hop: i + 1,
         fromOwner: owners[i],
         fromAccount: accounts[i].accountNumber,
         toOwner: owners[i + 1],
         toAccount: accounts[i + 1].accountNumber,
         sentAt: hops[i].sentAt,
         outAmount: round(hops[i].outAmount * 100) / 100.0,
         inAmount: round(hops[i].inAmount * 100) / 100.0,
         dropVsPrevHop: round(
           (CASE WHEN i = 0 THEN 0.0 ELSE hops[i - 1].inAmount - hops[i].inAmount END) * 100
         ) / 100.0,
         retainedVsPrevPct: round(
           (CASE WHEN i = 0 THEN 1.0 ELSE hops[i].inAmount / hops[i - 1].inAmount END) * 1000
         ) / 10.0
       }] AS chain
ORDER BY hopCount DESC, retainedPct ASC
LIMIT 1
```

**5. Combined APP fraud score** — Score accounts by combining fan-in + rapid transit + chain participation:

```cypher
// Score accounts: fan-in points + rapid transit points
MATCH (sender:Customer)-[:OWNS]->(sAcct:Account)-[:RECORDED]->(debit:Transaction),
      (receiver:Customer)-[:OWNS]->(rAcct:Account)-[:RECORDED]->(credit:Transaction)
WHERE toFloat(debit.amount) < 0 AND toFloat(credit.amount) > 0
  AND debit.createdAt = credit.createdAt
  AND abs(toFloat(debit.amount)) = toFloat(credit.amount)
  AND sAcct <> rAcct AND sender <> receiver
  AND debit.transactionType = 'Transfer'
WITH receiver, rAcct,
     count(DISTINCT sender) AS uniqueSenders,
     collect(credit) AS credits
// Fan-in score: 0 if < 3, else (uniqueSenders - 2) * 10
WITH receiver, rAcct, uniqueSenders, credits,
     CASE WHEN uniqueSenders >= 3 THEN (uniqueSenders - 2) * 10 ELSE 0 END AS fanInScore
// Rapid transit score: count of in→out pairs within 24h
UNWIND credits AS c
OPTIONAL MATCH (rAcct)-[:RECORDED]->(rapidOut:Transaction)
WHERE toFloat(rapidOut.amount) < 0
  AND rapidOut.transactionType = 'Transfer'
  AND rapidOut.createdAt > c.createdAt
  AND datetime(rapidOut.createdAt) <= datetime(c.createdAt) + duration('PT24H')
WITH receiver, rAcct, fanInScore,
     count(rapidOut) AS rapidTransits
WITH receiver, rAcct,
     fanInScore + rapidTransits * 5 AS fraudScore
WHERE fraudScore > 0
RETURN receiver.firstName + ' ' + receiver.lastName AS suspect,
       rAcct.accountNumber AS account,
       fraudScore
ORDER BY fraudScore DESC
```
