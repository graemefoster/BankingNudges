using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankOfGraeme.Api.Services;

public record CustomerContext(
    CustomerInfo Customer,
    FinancialInfo Financial,
    List<UpcomingPayment> Upcoming,
    List<NudgeSignal> Signals);

public record CustomerInfo(
    int Id,
    string Name,
    int NudgeFatigueThisWeek);

public record AccountInfo(
    string Name,
    string AccountType,
    decimal Balance,
    decimal? InterestRate,
    decimal? BonusInterestRate,
    decimal? OffsetHomeLoanRate = null);

public record FinancialInfo(
    decimal CurrentBalance,
    decimal AvgMonthlyIncome,
    Dictionary<string, decimal> SpendByCategory,
    Dictionary<string, double> SpendDelta,
    int DaysUntilLikelyPayday,
    List<AccountInfo>? Accounts = null);

public class NudgeContextAssembler(
    BankDbContext db,
    IDateTimeProvider dateTime,
    NudgePatternDetector patternDetector,
    NudgeSignalDetector signalDetector,
    ILogger<NudgeContextAssembler> logger)
{
    public async Task<CustomerContext?> AssembleAsync(int customerId)
    {
        var now = dateTime.UtcNow;
        var today = dateTime.Today;

        var customer = await db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId);

        if (customer is null)
        {
            logger.LogWarning("Customer {CustomerId} not found", customerId);
            return null;
        }

        var accounts = await db.Accounts
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId && a.IsActive)
            .ToListAsync();

        var accountIds = accounts.Select(a => a.Id).ToList();

        var ninetyDaysAgo = now.AddDays(-90);
        var transactions = await db.Transactions
            .AsNoTracking()
            .Where(t => accountIds.Contains(t.AccountId) && t.CreatedAt >= ninetyDaysAgo)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        // Include Transaction, Savings, and Offset accounts in usable balance.
        // Exclude HomeLoan accounts (they carry the loan debt, not usable cash).
        // Offset account balances are fully usable — the money is the customer's
        // everyday cash that also happens to reduce home loan interest.
        var currentBalance = 0m;
        foreach (var acct in accounts.Where(a => a.AccountType is AccountType.Transaction or AccountType.Savings or AccountType.Offset))
        {
            currentBalance += acct.Balance;
        }

        var scheduledPayments = await db.ScheduledPayments
            .AsNoTracking()
            .Where(sp => accountIds.Contains(sp.AccountId) && sp.IsActive)
            .ToListAsync();

        var weekAgo = now.AddDays(-7);
        var nudgesThisWeek = await db.Nudges
            .AsNoTracking()
            .CountAsync(n => n.CustomerId == customerId && n.CreatedAt >= weekAgo);

        // Spend by category — last 30 days
        var thirtyDaysAgo = now.AddDays(-30);
        var sixtyDaysAgo = now.AddDays(-60);

        var recentTxns = transactions.Where(t => t.CreatedAt >= thirtyDaysAgo).ToList();
        var priorTxns = transactions.Where(t => t.CreatedAt >= sixtyDaysAgo && t.CreatedAt < thirtyDaysAgo).ToList();

        var spendByCategory = CalculateSpendByCategory(recentTxns);
        var priorSpendByCategory = CalculateSpendByCategory(priorTxns);
        var spendDelta = CalculateSpendDelta(spendByCategory, priorSpendByCategory);

        // Estimate monthly income from regular large credits
        var avgMonthlyIncome = EstimateMonthlyIncome(transactions);

        // Detect recurring patterns from transaction history
        var inferredRecurring = patternDetector.DetectRecurringPatterns(transactions);

        // Merge scheduled payments with inferred recurring, deduplicate
        var upcoming = BuildUpcomingPayments(scheduledPayments, inferredRecurring, today);

        // Estimate days until payday
        var daysUntilPayday = EstimateDaysUntilPayday(transactions, now);

        // Estimate average monthly expenses
        var avgMonthlyExpenses = recentTxns
            .Where(t => t.Amount < 0)
            .Sum(t => Math.Abs(t.Amount));

        // Detect signals
        var signals = signalDetector.DetectSignals(
            currentBalance, upcoming, spendDelta, spendByCategory, avgMonthlyExpenses, daysUntilPayday);

        var accountInfos = accounts
            .Where(a => a.AccountType is AccountType.Transaction or AccountType.Savings or AccountType.Offset)
            .Select(a =>
            {
                decimal? offsetHomeLoanRate = null;
                if (a.AccountType == AccountType.Offset && a.HomeLoanAccountId.HasValue)
                {
                    var linkedLoan = accounts.FirstOrDefault(l => l.Id == a.HomeLoanAccountId.Value);
                    offsetHomeLoanRate = linkedLoan?.InterestRate;
                }

                return new AccountInfo(
                    a.Name,
                    a.AccountType.ToString(),
                    a.Balance,
                    a.InterestRate,
                    a.BonusInterestRate,
                    offsetHomeLoanRate);
            })
            .ToList();

        return new CustomerContext(
            Customer: new CustomerInfo(
                Id: customer.Id,
                Name: customer.FullName,
                NudgeFatigueThisWeek: nudgesThisWeek),
            Financial: new FinancialInfo(
                CurrentBalance: currentBalance,
                AvgMonthlyIncome: avgMonthlyIncome,
                SpendByCategory: spendByCategory,
                SpendDelta: spendDelta,
                DaysUntilLikelyPayday: daysUntilPayday,
                Accounts: accountInfos),
            Upcoming: upcoming,
            Signals: signals);
    }

    private static Dictionary<string, decimal> CalculateSpendByCategory(List<Transaction> transactions)
    {
        var result = new Dictionary<string, decimal>();
        var debits = transactions.Where(t => t.Amount < 0);

        foreach (var txn in debits)
        {
            var category = CategoriseTransaction(txn.Description);
            result.TryAdd(category, 0);
            result[category] += Math.Abs(txn.Amount);
        }

        return result;
    }

    private static Dictionary<string, double> CalculateSpendDelta(
        Dictionary<string, decimal> current,
        Dictionary<string, decimal> prior)
    {
        // If there's no prior spending history at all (e.g. new account < 30 days old),
        // deltas are meaningless — every category would look like a 100% spike.
        if (prior.Count == 0)
            return new Dictionary<string, double>();

        var delta = new Dictionary<string, double>();
        var allCategories = current.Keys.Union(prior.Keys);

        foreach (var cat in allCategories)
        {
            var currentAmount = current.GetValueOrDefault(cat, 0m);
            var priorAmount = prior.GetValueOrDefault(cat, 0m);

            if (priorAmount > 0)
            {
                delta[cat] = (double)((currentAmount - priorAmount) / priorAmount);
            }
            else if (currentAmount > 0)
            {
                delta[cat] = 1.0; // 100% increase from zero
            }
        }

        return delta;
    }

    private static string CategoriseTransaction(string description) =>
        MerchantCategoryMapper.Categorise(description);

    private static decimal EstimateMonthlyIncome(List<Transaction> transactions)
    {
        // Look for regular large credits (salary patterns)
        var credits = transactions
            .Where(t => t.Amount > 500 && t.TransactionType == TransactionType.Deposit)
            .ToList();

        if (credits.Count == 0) return 0;

        var months = Math.Max(1, (transactions.Max(t => t.CreatedAt) - transactions.Min(t => t.CreatedAt)).TotalDays / 30.0);
        return Math.Round(credits.Sum(c => c.Amount) / (decimal)months, 2);
    }

    private static int EstimateDaysUntilPayday(List<Transaction> transactions, DateTime now)
    {
        var salaryCredits = transactions
            .Where(t => t.Amount > 500 && t.TransactionType == TransactionType.Deposit)
            .OrderByDescending(t => t.CreatedAt)
            .Take(6)
            .ToList();

        if (salaryCredits.Count < 2) return 14; // default if insufficient data

        var gaps = new List<double>();
        for (var i = 0; i < salaryCredits.Count - 1; i++)
        {
            gaps.Add((salaryCredits[i].CreatedAt - salaryCredits[i + 1].CreatedAt).TotalDays);
        }

        var avgGap = gaps.Average();
        var lastPay = salaryCredits[0].CreatedAt;
        var nextExpected = lastPay.AddDays(avgGap);

        var daysUntil = (int)Math.Ceiling((nextExpected - now).TotalDays);
        return Math.Max(0, daysUntil);
    }

    private static List<UpcomingPayment> BuildUpcomingPayments(
        List<ScheduledPayment> scheduled,
        List<RecurringPattern> inferred,
        DateOnly today)
    {
        var sevenDaysFromNow = today.AddDays(7);
        var upcoming = new Dictionary<string, UpcomingPayment>(StringComparer.OrdinalIgnoreCase);

        // Scheduled payments take precedence
        foreach (var sp in scheduled)
        {
            if (sp.NextDueDate <= sevenDaysFromNow)
            {
                var dueIn = sp.NextDueDate.DayNumber - today.DayNumber;
                if (dueIn < 0) continue;

                upcoming[sp.PayeeName.ToUpperInvariant()] = new UpcomingPayment(
                    Merchant: sp.PayeeName,
                    Amount: sp.Amount,
                    DueInDays: dueIn,
                    Confidence: "SCHEDULED",
                    Source: "ScheduledPayment");
            }
        }

        // Add inferred patterns that don't overlap with scheduled
        // Dedup criteria: name substring match (min 4 chars to avoid false positives
        // like "BP" matching "ABP") AND amount within 20%.
        // Future improvements if needed:
        //   - Use Levenshtein/Jaro-Winkler distance for fuzzy name matching
        //   - Match on BSB+account number when available on both sides
        //   - Use the scheduled payment's linked AccountId to match transactions
        foreach (var pattern in inferred)
        {
            var nextDue = DateOnly.FromDateTime(pattern.NextExpectedDate);
            if (nextDue > sevenDaysFromNow) continue;

            var dueIn = nextDue.DayNumber - today.DayNumber;
            if (dueIn < 0) continue;

            var key = pattern.Merchant.ToUpperInvariant();
            var isDuplicate = upcoming.Any(kvp =>
            {
                var shorter = kvp.Key.Length < key.Length ? kvp.Key : key;
                var longer = kvp.Key.Length < key.Length ? key : kvp.Key;
                // Require the shorter name to be at least 4 chars and be a substring of the longer
                var nameMatch = shorter.Length >= 4 && longer.Contains(shorter);
                if (!nameMatch) return false;
                var amountRatio = pattern.AvgAmount > 0
                    ? (double)(kvp.Value.Amount / pattern.AvgAmount)
                    : 0;
                return amountRatio is > 0.8 and < 1.2;
            });

            if (!isDuplicate)
            {
                upcoming[key] = new UpcomingPayment(
                    Merchant: pattern.Merchant,
                    Amount: pattern.AvgAmount,
                    DueInDays: dueIn,
                    Confidence: pattern.Confidence.ToString(),
                    Source: "InferredPattern");
            }
        }

        return upcoming.Values
            .OrderBy(p => p.DueInDays)
            .ToList();
    }
}
