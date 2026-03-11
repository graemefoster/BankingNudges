# Bank of Graeme — Nudge Signal Logic

How the nudge system decides **what** to tell a customer and **when**.

> **Source code:** `src/BankOfGraeme.Domain/Services/NudgeSignalDetector.cs` (signal detection) and `NudgeGenerator.cs` (LLM prompt & validation).

---

## 1. Inputs

The signal detector receives five inputs each time it runs:

| Input | Type | Source |
|-------|------|--------|
| `currentBalance` | decimal | Customer's transaction account balance |
| `upcomingPayments` | list | Predicted payments in the next 7 days (merchant, amount, days until due, confidence) |
| `spendDelta` | dictionary | Category → % change vs. prior month (e.g. `Dining: +0.65` = up 65%). **Empty when the account has no prior-month history** (see [new account handling](#7-new-account-handling)). |
| `spendByCategory` | dictionary | Category → actual dollar amount spent this month |
| `avgMonthlyExpenses` | decimal | Average total monthly outgoings |
| `daysUntilPayday` | int | Estimated days until next income deposit |

---

## 2. Derived Flags

Before evaluating individual signals, the detector computes two boolean flags that **gate** several signals:

### `canComfortablyCoverUpcoming`

```
canComfortablyCoverUpcoming = totalUpcoming == 0
                            || currentBalance >= totalUpcoming × 2
```

True when the customer's balance is at least **double** their upcoming payments (or there are no upcoming payments). This is used to suppress `PAYMENT_DUE_SOON` and to lower `SPEND_SPIKE` severity.

### `balanceIsHealthy`

```
balanceIsHealthy = canComfortablyCoverUpcoming
                 && avgMonthlyExpenses > 0
                 && currentBalance > avgMonthlyExpenses × 1.5
```

True when the customer can cover upcoming payments **and** has 1.5× their average monthly expenses in the bank. When true, `SPEND_SPIKE` signals are suppressed entirely — there's no reason to flag higher spending when cash reserves are strong.

---

## 3. Signal Types

### `LOW_BALANCE` — Balance is below $500

| Field | Value |
|-------|-------|
| **Condition** | `currentBalance < $500` |
| **Severity** | HIGH |
| **Gated by** | _(none — always evaluated)_ |

A blunt threshold. Fires regardless of upcoming payments or spending patterns. Note: this does **not** imply the customer can't cover any specific payment — see the [worked example](#5-worked-example) below.

### `CANT_COVER_UPCOMING` — Balance won't cover upcoming payments

| Field | Value |
|-------|-------|
| **Condition** | `currentBalance < totalUpcoming × 1.1` |
| **Severity** | HIGH |
| **Gated by** | _(none — always evaluated)_ |

Fires when the balance is less than 110% of total upcoming payments. This is the **only** signal that means the customer may actually miss a payment.

### `PAYMENT_DUE_SOON` — An imminent payment when cash is tight

| Field | Value |
|-------|-------|
| **Condition** | Payment due within 2 days |
| **Severity** | MEDIUM |
| **Gated by** | `!canComfortablyCoverUpcoming` |

Only fires when the balance is **not** comfortably covering upcoming payments (i.e. balance < 2× upcoming total). Emits one signal per imminent payment, including merchant name, amount, and days until due.

### `SPEND_SPIKE` — Spending jumped in a category

| Field | Value |
|-------|-------|
| **Condition** | Category spend up > 40% vs. last month **and** category amount ≥ 5% of avg monthly expenses |
| **Severity** | LOW (if can cover upcoming) or MEDIUM (if can't) |
| **Gated by** | `!balanceIsHealthy` |

Only fires when the customer's overall financial position is under some pressure. If the balance is healthy (1.5× monthly expenses and 2× upcoming), spending increases are not flagged at all.

Additionally, a **materiality filter** ignores spikes in categories where the actual dollar amount is trivial. For example, entertainment going from $5 to $8 is a 60% spike but only $8/month — if the customer's average monthly expenses are $3,000, that's 0.27% and not worth flagging. The threshold is 5% of average monthly expenses (e.g. $150 for a $3,000/month customer).

### `EXCESS_CASH_SITTING` — Large idle balance

| Field | Value |
|-------|-------|
| **Condition** | `currentBalance > avgMonthlyExpenses × 1.5` |
| **Severity** | LOW |
| **Gated by** | _(none — always evaluated)_ |

Fires when the balance significantly exceeds typical monthly needs. This is informational — the LLM prompt rules prevent it from being turned into investment advice.

### `PAYDAY_INCOMING` — Pay day is tomorrow (or today)

| Field | Value |
|-------|-------|
| **Condition** | `daysUntilPayday <= 1` |
| **Severity** | LOW |
| **Gated by** | _(none — always evaluated)_ |

Contextual signal to help the LLM frame messages positively (e.g. "your balance is low, but income is expected tomorrow").

---

## 4. LLM Prompt Guardrails

Signals are passed to the LLM along with the customer's full financial context. The system prompt includes rules to prevent the LLM from generating misleading or harmful nudges:

| Guardrail | Why it exists |
|-----------|---------------|
| **No financial advice** — never tell the customer what to do ("consider moving", "you should", "try to") | Regulatory requirement: nudges are informational, not advisory |
| **No false payment urgency** — don't describe a balance as "close to" a payment when balance > 2× that payment | Prevents misleading correlation between `LOW_BALANCE` and unrelated small payments |
| **No spend-shaming when healthy** — don't suggest trimming costs when balance is strong | A spending increase is just information when there's plenty of cash |
| **No hallucinated numbers** — every $ amount in the nudge is verified against context data | Post-generation validation rejects nudges containing numbers not found in the input |
| **Max 40 words** — nudge messages must be concise | Validated after generation; overly long nudges are rejected |
| **No specific product recommendations** | Nudges should not push investment products or third-party services |

---

## 5. Worked Example

**Scenario:** Customer has $164.92 balance, $12.00 STAN payment due today, average monthly expenses $200.

### Signal evaluation

| Step | Check | Result |
|------|-------|--------|
| Derived: `totalUpcoming` | $12.00 | |
| Derived: `canComfortablyCoverUpcoming` | $164.92 ≥ $12 × 2 ($24) | ✅ **true** |
| Derived: `balanceIsHealthy` | true && $200 > 0 && $164.92 > $200 × 1.5 ($300) | ❌ **false** (balance below 1.5× expenses) |
| `LOW_BALANCE` | $164.92 < $500 | ✅ **fires** (HIGH) |
| `CANT_COVER_UPCOMING` | $164.92 < $12 × 1.1 ($13.20) | ❌ does not fire |
| `PAYMENT_DUE_SOON` | gated by `!canComfortablyCoverUpcoming` | ❌ **suppressed** (balance easily covers payments) |
| `SPEND_SPIKE` | gated by `!balanceIsHealthy`, then checks deltas | may fire if a category is up > 40% |
| `EXCESS_CASH_SITTING` | $164.92 > $200 × 1.5 ($300) | ❌ does not fire |
| `PAYDAY_INCOMING` | depends on payday estimate | depends on data |

### What the LLM receives

- **Signals:** `LOW_BALANCE` (balance $164.92)
- **Upcoming payments:** STAN $12.00 due in 0 days
- **Balance:** $164.92

### What went wrong (before the fix)

The LLM saw `LOW_BALANCE` + the STAN payment and wrote: *"Your current balance of $164.92 is close to the upcoming $12.00 payment for STAN due today."*

This is misleading — $164.92 is **nearly 14×** the $12 payment. The `CANT_COVER_UPCOMING` signal did not fire, meaning the system knew the customer could comfortably pay. The LLM just made a false inference.

### The fix

A prompt guardrail was added:

> *Do NOT describe a balance as "close to", "tight for", or "just enough for" a specific payment when the balance is more than double the payment amount. Only reference a specific payment in a low-balance context when the CANT_COVER_UPCOMING signal is active.*

With this rule, the LLM should instead generate something like: *"Your account balance is $164.92."* — factual, no false urgency.

---

## 6. Constants Reference

| Constant | Value | Used by |
|----------|-------|---------|
| `LowBalanceThreshold` | $500 | `LOW_BALANCE` |
| `SpendSpikeThreshold` | 40% | `SPEND_SPIKE` |
| `SpendSpikeMaterialityThreshold` | 5% of avg monthly expenses | `SPEND_SPIKE` — ignore trivial categories |
| Comfortable cover multiplier | 2× upcoming total | `canComfortablyCoverUpcoming` |
| Healthy balance multiplier | 1.5× avg monthly expenses | `balanceIsHealthy` |
| Can't-cover buffer | 1.1× upcoming total | `CANT_COVER_UPCOMING` |
| Payment imminence window | ≤ 2 days | `PAYMENT_DUE_SOON` |
| Payday window | ≤ 1 day | `PAYDAY_INCOMING` |
| Max nudges per week | 3 | `NudgeGenerator` fatigue check |
| Max message words | 40 | `NudgeGenerator` validation |

---

## 7. New Account Handling

The spend delta calculation compares spending in the last 30 days against spending in days 30–60. For accounts younger than ~30 days, the prior window is empty — **every** category would show a 100% increase from $0, triggering false `SPEND_SPIKE` signals across the board.

**Guard:** When the prior spending dictionary is completely empty (no history in the 30–60 day window), `CalculateSpendDelta` returns an empty dictionary. This means no spend deltas are computed and no `SPEND_SPIKE` signals can fire.

**Effect:** New customers will still receive other relevant signals (`LOW_BALANCE`, `PAYDAY_INCOMING`, etc.) but won't be told "spending is up across all categories" when there's no baseline to compare against.

---

## 8. One Nudge Per Day

Both the on-demand endpoint and the batch runner enforce a **one-nudge-per-customer-per-day** rule. If a nudge has already been generated for a customer today (regardless of whether it was accepted, dismissed, or is still pending), no new nudge will be created.

- **On-demand (`POST /api/nudges/generate/:customerId`)**: Returns the existing nudge from today instead of generating a duplicate.
- **Batch runner**: Skips customers who already have a nudge created today.

This prevents the same insight being repeated with slightly different wording when the underlying signals haven't changed. The weekly fatigue limit (max 3 per 7-day window) provides an additional broader cap.
