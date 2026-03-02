using BankOfGraeme.Api.Models;
using BankOfGraeme.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BankOfGraeme.Api.Data;

public static class SeedData
{
    private const int CustomerCount = 10_000;
    private const int RandomSeed = 42;
    private const string Bsb = "062-000";

    // Batch sizes for bulk insert performance
    private const int CustomerBatchSize = 500;
    private const int TransactionBatchSize = 10_000;

    #region Lookup Data

    private static readonly string[] FirstNames =
    [
        "Oliver", "Charlotte", "Jack", "Amelia", "Noah", "Isla", "William", "Mia", "James", "Ava",
        "Thomas", "Grace", "Lucas", "Chloe", "Henry", "Olivia", "Ethan", "Sophie", "Alexander", "Emily",
        "Liam", "Harper", "Mason", "Ella", "Sebastian", "Lily", "Benjamin", "Zoe", "Archer", "Ruby",
        "Leo", "Matilda", "Hudson", "Willow", "Theodore", "Ivy", "Harrison", "Sienna", "Hunter", "Aria",
        "Cooper", "Evie", "Charlie", "Scarlett", "Lachlan", "Audrey", "Riley", "Layla", "Oscar", "Piper",
        "Max", "Poppy", "Samuel", "Frankie", "Daniel", "Luna", "Patrick", "Savannah", "Ryan", "Hannah",
        "Finn", "Mackenzie", "George", "Ellie", "Kai", "Violet", "Nathan", "Billie", "Jake", "Aurora",
        "Caleb", "Hazel", "Mitchell", "Stella", "Dylan", "Georgia", "Harvey", "Ayla", "Angus", "Mila",
        "Connor", "Daisy", "Joshua", "Harlow", "Marcus", "Penelope", "Declan", "Summer", "Flynn", "Jasmine",
        "Bailey", "Freya", "Hamish", "Adelaide", "Ashton", "Kiara", "Levi", "Maya", "Owen", "Eden",
        "Xavier", "Phoebe", "Nate", "Wren", "Jaxon", "Alice", "Felix", "Rosie", "Hugo", "Clara",
        "Ryder", "Lila", "Bodhi", "Thea", "Eli", "Elsie", "Blake", "Bonnie", "Logan", "Imogen",
        "Tyler", "Harriet", "Beau", "Abigail", "Louis", "Sadie", "Jesse", "Eloise", "Spencer", "Sage",
        "Cameron", "Indie", "Jayden", "Lydia", "Callum", "Margot", "Aiden", "Olive", "Toby", "Quinn",
        "Maxwell", "Arabella", "Dominic", "Claire", "Luca", "Nora", "Joel", "Florence", "Reid", "Paige",
        "Gabriel", "Heidi", "Jasper", "Darcy", "Miles", "Emilia", "Edward", "Lottie", "Heath", "Millie",
        "Roman", "Anastasia", "Rafferty", "Georgie", "Lennox", "Asher", "Cohen", "Phoenix", "Elijah", "Ziggy",
        "Hendrix", "Sonny", "Kingston", "Cruz", "Ellis", "Otis", "Atlas", "Rocco", "Arlo", "Mack",
        "Brodie", "Tate", "Weston", "Sullivan", "Beckett", "Wells", "Nixon", "Zane", "Rhodes", "Denver",
        "Clyde", "Sterling", "Magnus", "Rowan", "Quinn", "Sawyer", "River", "Avery", "Dakota", "Hayden"
    ];

    private static readonly string[] LastNames =
    [
        "Smith", "Jones", "Williams", "Brown", "Wilson", "Taylor", "Johnson", "White", "Martin", "Anderson",
        "Thompson", "Nguyen", "Thomas", "Walker", "Harris", "Lee", "Ryan", "Robinson", "Kelly", "King",
        "Chen", "Davis", "Wright", "Clark", "Hall", "Young", "Mitchell", "Green", "Campbell", "Edwards",
        "Turner", "Roberts", "Parker", "Evans", "Collins", "Murphy", "Morris", "Cook", "Rogers", "Morgan",
        "Cooper", "Richardson", "Watson", "Brooks", "Wood", "James", "Stewart", "Scott", "McDonald", "Singh",
        "Ward", "Reid", "Ross", "Bennett", "Gray", "Fraser", "Hamilton", "Murray", "Marshall", "Patel",
        "Hughes", "Bell", "Baker", "Shaw", "Ali", "Adams", "Chapman", "Grant", "Simpson", "Li",
        "Kennedy", "Palmer", "Gibson", "Webb", "O'Brien", "Russell", "Barker", "Sullivan", "Henderson", "Cole",
        "Mason", "Hart", "Dunn", "Wang", "Fox", "Hunt", "Price", "Carter", "Bailey", "Burton",
        "Fisher", "Black", "Graham", "Pearce", "Dixon", "Stone", "Knight", "Burke", "Doyle", "Long",
        "Zhang", "Burns", "Huynh", "Tran", "Lam", "Kaur", "Sharma", "Doherty", "Lynch", "O'Connor",
        "Gallagher", "Fitzgerald", "Brennan", "Walsh", "Payne", "Dawson", "Stephens", "Watts", "Miles", "Cross",
        "Blake", "Reeves", "Lawrence", "Page", "Holland", "Barton", "Marsh", "Chambers", "Armstrong", "Carr",
        "Owen", "Day", "Todd", "Willis", "Booth", "Craig", "Gordon", "Pearson", "Griffiths", "Lowe",
        "Bolton", "Conway", "Elliott", "Lane", "Bates", "Holt", "Frost", "Lamb", "Fields", "Barber",
        "Arnold", "Fleming", "Nicholson", "Howe", "Bowman", "Rhodes", "Harper", "Saunders", "Curtis", "Howell",
        "Moran", "Dodd", "Steele", "Quinn", "Cooke", "Bryan", "Haynes", "Stanley", "Osborne", "Benson",
        "Finch", "Daly", "Kemp", "Maher", "Power", "Nolan", "Barry", "Tierney", "Carey", "Buckley",
        "Duffy", "Egan", "Flynn", "Foley", "Hennessy", "Keane", "Mahoney", "McBride", "McLean", "Milne",
        "Pierce", "Rowe", "Sutton", "Tucker", "Underwood", "Vaughan", "Winter", "Wolfe", "Yang", "Zhou"
    ];

    private static readonly string[] TransactionAccountNames =
        ["Everyday Transaction", "Smart Access", "Complete Freedom", "Choice Account", "Everyday Account"];

    private static readonly string[] SavingsAccountNames =
        ["Goal Saver", "NetBank Saver", "Bonus Saver", "Future Saver", "Rainy Day Fund"];

    private static readonly string[] HomeLoanNames =
        ["Home Loan", "Standard Variable Loan", "Fixed Rate Loan", "Investment Loan"];

    private static readonly string[] OffsetAccountNames =
        ["Offset Account", "Mortgage Offset", "Offset Saver"];

    // Debit merchants with (description, min amount, max amount)
    private static readonly (string Desc, decimal Min, decimal Max)[] SupermarketMerchants =
    [
        ("Woolworths", 15m, 250m), ("Coles", 15m, 250m), ("Aldi", 10m, 180m),
        ("IGA", 8m, 120m), ("Harris Farm Markets", 20m, 150m)
    ];

    private static readonly (string Desc, decimal Min, decimal Max)[] FuelMerchants =
    [
        ("BP", 30m, 120m), ("Shell", 30m, 120m), ("Ampol", 30m, 120m),
        ("7-Eleven Fuel", 25m, 100m)
    ];

    private static readonly (string Desc, decimal Min, decimal Max)[] FoodDrinkMerchants =
    [
        ("McDonald's", 8m, 30m), ("Coffee Club", 4m, 12m), ("Guzman y Gomez", 12m, 25m),
        ("Uber Eats", 15m, 55m), ("Menulog", 18m, 50m), ("Domino's Pizza", 10m, 40m),
        ("KFC", 8m, 28m), ("Nando's", 15m, 35m), ("The Coffee Bean", 4m, 9m),
        ("Gloria Jean's", 5m, 12m), ("Hungry Jack's", 8m, 25m), ("Sushi Hub", 10m, 22m)
    ];

    private static readonly (string Desc, decimal Min, decimal Max)[] SubscriptionMerchants =
    [
        ("Netflix", 16.99m, 25.99m), ("Spotify", 12.99m, 12.99m), ("Stan", 12m, 21m),
        ("Disney+", 13.99m, 13.99m), ("Apple.com/bill", 7.99m, 22.99m),
        ("Amazon Prime", 9.99m, 9.99m), ("YouTube Premium", 16.99m, 22.99m),
        ("Kayo Sports", 27.99m, 27.99m), ("Binge", 10m, 18m)
    ];

    private static readonly (string Desc, decimal Min, decimal Max)[] UtilityMerchants =
    [
        ("AGL Energy", 80m, 350m), ("Origin Energy", 80m, 350m), ("Sydney Water", 60m, 200m),
        ("Telstra", 49m, 150m), ("Optus", 39m, 120m), ("Vodafone", 35m, 100m)
    ];

    private static readonly (string Desc, decimal Min, decimal Max)[] ShoppingMerchants =
    [
        ("Kmart", 10m, 120m), ("Target", 15m, 150m), ("Bunnings", 10m, 300m),
        ("JB Hi-Fi", 20m, 500m), ("Big W", 10m, 100m), ("Myer", 20m, 250m),
        ("David Jones", 30m, 400m), ("Cotton On", 15m, 80m), ("Chemist Warehouse", 8m, 80m)
    ];

    private static readonly (string Desc, decimal Min, decimal Max)[] HealthMerchants =
    [
        ("Priceline Pharmacy", 8m, 60m), ("Dr Smith Medical", 40m, 90m),
        ("Physio Works", 60m, 95m), ("Dental Care", 50m, 300m)
    ];

    private static readonly (string Desc, decimal Min, decimal Max)[] TransportMerchants =
    [
        ("Opal Top Up", 20m, 50m), ("Uber", 8m, 55m), ("DiDi", 8m, 45m),
        ("Wilson Parking", 8m, 35m), ("Secure Parking", 10m, 30m)
    ];

    // Weighted transaction categories for Transaction accounts (weights sum to 100)
    private static readonly (int Weight, (string Desc, decimal Min, decimal Max)[] Merchants)[] DebitCategories =
    [
        (25, SupermarketMerchants), (10, FuelMerchants), (20, FoodDrinkMerchants),
        (8, SubscriptionMerchants), (7, UtilityMerchants), (12, ShoppingMerchants),
        (8, HealthMerchants), (10, TransportMerchants)
    ];

    private static readonly (string Desc, decimal Min, decimal Max)[] OffsetBills =
    [
        ("Council Rates", 300m, 600m), ("Home Insurance", 150m, 350m),
        ("Strata Levy", 500m, 1200m), ("Water Bill", 80m, 200m),
        ("Land Tax", 200m, 500m), ("Body Corporate", 400m, 900m)
    ];

    #endregion

    public static void Seed(BankDbContext db, IDateTimeProvider dateTime)
    {
        // Seed staff users independently so they're available in upgraded DBs
        if (!db.StaffUsers.Any())
        {
            db.StaffUsers.AddRange(
                new StaffUser
                {
                    Username = "admin",
                    DisplayName = "Admin User",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
                    Role = "admin"
                },
                new StaffUser
                {
                    Username = "teller",
                    DisplayName = "Jane Teller",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("teller"),
                    Role = "teller"
                }
            );
            db.SaveChanges();
        }

        if (db.Customers.Any()) return;

        var rng = new Random(RandomSeed);
        var now = dateTime.UtcNow;
        var originalAutoDetect = db.ChangeTracker.AutoDetectChangesEnabled;
        db.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            GenerateAllData(db, rng, now);
        }
        finally
        {
            db.ChangeTracker.AutoDetectChangesEnabled = originalAutoDetect;
        }
    }

    private static void GenerateAllData(BankDbContext db, Random rng, DateTime now)
    {
        // Phase 1: Generate and insert all customers in batches
        var allCustomers = new List<Customer>(CustomerCount);
        for (int i = 0; i < CustomerCount; i++)
        {
            allCustomers.Add(GenerateCustomer(rng, i, now));
        }

        foreach (var batch in allCustomers.Chunk(CustomerBatchSize))
        {
            db.Customers.AddRange(batch);
            db.SaveChanges();
            db.ChangeTracker.Clear();
        }

        // Phase 2: Generate accounts for each customer
        // We need IDs assigned, so reload customer IDs
        var customerIds = db.Customers.Select(c => c.Id).OrderBy(id => id).ToList();

        // Reset RNG to a known state for account generation
        var accountRng = new Random(RandomSeed + 1);
        var allAccounts = new List<Account>();
        var offsetLinks = new List<(int OffsetIndex, int HomeLoanIndex)>();

        int accountIndex = 0;
        foreach (var customerId in customerIds)
        {
            var accountCount = PickAccountCount(accountRng);
            var accountAgeDays = PickAccountAgeDays(accountRng);
            var customerAccounts = GenerateCustomerAccounts(
                accountRng, customerId, accountCount, accountAgeDays, now, ref accountIndex);

            // Track offset→homeloan links within this customer's accounts
            int? homeLoanIdx = null;
            int? offsetIdx = null;
            for (int j = 0; j < customerAccounts.Count; j++)
            {
                var globalIdx = allAccounts.Count + j;
                if (customerAccounts[j].AccountType == AccountType.HomeLoan) homeLoanIdx = globalIdx;
                if (customerAccounts[j].AccountType == AccountType.Offset) offsetIdx = globalIdx;
            }
            if (homeLoanIdx.HasValue && offsetIdx.HasValue)
                offsetLinks.Add((offsetIdx.Value, homeLoanIdx.Value));

            allAccounts.AddRange(customerAccounts);
        }

        // Insert accounts in batches
        foreach (var batch in allAccounts.Chunk(CustomerBatchSize))
        {
            db.Accounts.AddRange(batch);
            db.SaveChanges();
            db.ChangeTracker.Clear();
        }

        // Phase 3: Link offset accounts to their home loans
        // Reload all account IDs in insertion order
        var accountIds = db.Accounts.OrderBy(a => a.Id).Select(a => new { a.Id, a.AccountType }).ToList();
        foreach (var (offsetIdx, homeLoanIdx) in offsetLinks)
        {
            var offset = db.Accounts.Find(accountIds[offsetIdx].Id)!;
            offset.HomeLoanAccountId = accountIds[homeLoanIdx].Id;
        }
        db.SaveChanges();
        db.ChangeTracker.Clear();

        // Phase 4: Generate transactions for each account
        var txnRng = new Random(RandomSeed + 2);
        var txnBuffer = new List<Transaction>(TransactionBatchSize);

        // Reload accounts with metadata needed for transaction generation
        var accountMetas = db.Accounts
            .OrderBy(a => a.Id)
            .Select(a => new AccountMeta(a.Id, a.AccountType, a.CreatedAt, a.LoanAmount, a.InterestRate, a.LoanTermMonths))
            .ToList();

        foreach (var meta in accountMetas)
        {
            var transactions = GenerateTransactions(txnRng, meta, now);
            txnBuffer.AddRange(transactions);

            if (txnBuffer.Count >= TransactionBatchSize)
            {
                FlushTransactions(db, txnBuffer);
            }
        }

        // Flush remaining
        if (txnBuffer.Count > 0)
            FlushTransactions(db, txnBuffer);

        // Phase 5: Update account balances to match final transaction BalanceAfter
        UpdateAccountBalances(db);
    }

    private static void FlushTransactions(BankDbContext db, List<Transaction> buffer)
    {
        db.Transactions.AddRange(buffer);
        db.SaveChanges();
        db.ChangeTracker.Clear();
        buffer.Clear();
    }

    private static void UpdateAccountBalances(BankDbContext db)
    {
        // For each account, set Balance = BalanceAfter of most recent transaction
        // Use raw SQL for efficiency
        db.Database.ExecuteSqlRaw("""
            UPDATE "Accounts" a
            SET "Balance" = t."BalanceAfter"
            FROM (
                SELECT DISTINCT ON ("AccountId") "AccountId", "BalanceAfter"
                FROM "Transactions"
                ORDER BY "AccountId", "CreatedAt" DESC, "Id" DESC
            ) t
            WHERE a."Id" = t."AccountId"
        """);
    }

    private static Customer GenerateCustomer(Random rng, int index, DateTime now)
    {
        var firstName = FirstNames[rng.Next(FirstNames.Length)];
        var lastName = LastNames[rng.Next(LastNames.Length)];
        var dobYear = rng.Next(1955, 2006);
        var dobMonth = rng.Next(1, 13);
        var dobDay = rng.Next(1, DateTime.DaysInMonth(dobYear, dobMonth) + 1);
        var phoneMiddle = rng.Next(1000, 10000);
        var phoneLast = rng.Next(1000, 10000);
        // Spread CreatedAt across the account age range — will be overwritten if needed
        var createdDaysAgo = rng.Next(30, 1461); // 1 month to 4 years

        return new Customer
        {
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName.ToLower()}.{lastName.ToLower()}{index}@email.com.au",
            Phone = $"04{rng.Next(10, 100):D2} {phoneMiddle:D3} {phoneLast:D3}",
            DateOfBirth = new DateOnly(dobYear, dobMonth, dobDay),
            CreatedAt = now.AddDays(-createdDaysAgo)
        };
    }

    private static int PickAccountCount(Random rng)
    {
        var roll = rng.Next(100);
        return roll switch
        {
            < 20 => 1,
            < 60 => 2,
            < 85 => 3,
            _ => 4
        };
    }

    private static int PickAccountAgeDays(Random rng)
    {
        var roll = rng.Next(100);
        return roll switch
        {
            < 25 => rng.Next(30, 91),     // 1-3 months
            < 60 => rng.Next(91, 548),     // 3-18 months
            < 85 => rng.Next(548, 1096),   // 18-36 months
            _ => rng.Next(1096, 1461)      // 36-48 months
        };
    }

    private static List<Account> GenerateCustomerAccounts(
        Random rng, int customerId, int accountCount, int ageDays, DateTime now, ref int accountIndex)
    {
        var accounts = new List<Account>();

        for (int i = 0; i < accountCount; i++)
        {
            var type = i switch
            {
                0 => AccountType.Transaction,
                1 => rng.Next(100) < 65 ? AccountType.Savings : AccountType.HomeLoan,
                2 => accounts.Any(a => a.AccountType == AccountType.HomeLoan)
                    ? AccountType.Savings
                    : (rng.Next(100) < 50 ? AccountType.HomeLoan : AccountType.Savings),
                _ => AccountType.Offset // 4th account is always Offset if they have a HomeLoan
            };

            // 4th account can only be Offset if they have a HomeLoan
            if (i == 3 && !accounts.Any(a => a.AccountType == AccountType.HomeLoan))
                type = AccountType.Savings;

            var accountNum = $"{accountIndex:D8}";
            accountIndex++;

            var name = type switch
            {
                AccountType.Transaction => TransactionAccountNames[rng.Next(TransactionAccountNames.Length)],
                AccountType.Savings => SavingsAccountNames[rng.Next(SavingsAccountNames.Length)],
                AccountType.HomeLoan => HomeLoanNames[rng.Next(HomeLoanNames.Length)],
                AccountType.Offset => OffsetAccountNames[rng.Next(OffsetAccountNames.Length)],
                _ => "Account"
            };

            // Slightly vary age per account (earlier accounts are older)
            var thisAgeDays = Math.Max(30, ageDays - (i * rng.Next(0, 60)));
            var createdAt = now.AddDays(-thisAgeDays);

            var account = new Account
            {
                CustomerId = customerId,
                AccountType = type,
                Bsb = Bsb,
                AccountNumber = accountNum,
                Name = name,
                Balance = 0m, // Will be set after transactions are generated
                CreatedAt = createdAt
            };

            if (type == AccountType.HomeLoan)
            {
                var loanAmount = (decimal)(rng.Next(250, 901) * 1000); // $250k-$900k
                account.LoanAmount = loanAmount;
                account.InterestRate = 4.5m + (decimal)(rng.Next(0, 30)) / 10m; // 4.5%-7.4%
                account.LoanTermMonths = rng.Next(100) < 80 ? 360 : 240; // 30 or 20 years
            }

            if (type == AccountType.Savings)
            {
                account.InterestRate = 2.0m + (decimal)(rng.Next(0, 35)) / 10m; // 2.0%-5.4%
            }

            accounts.Add(account);
        }

        return accounts;
    }

    private static List<Transaction> GenerateTransactions(Random rng, AccountMeta meta, DateTime now)
    {
        return meta.AccountType switch
        {
            AccountType.Transaction => GenerateTransactionAccountTxns(rng, meta, now),
            AccountType.Savings => GenerateSavingsAccountTxns(rng, meta, now),
            AccountType.HomeLoan => GenerateHomeLoanTxns(rng, meta, now),
            AccountType.Offset => GenerateOffsetAccountTxns(rng, meta, now),
            _ => []
        };
    }

    private static List<Transaction> GenerateTransactionAccountTxns(Random rng, AccountMeta meta, DateTime now)
    {
        var txns = new List<Transaction>();
        var balance = (decimal)(rng.Next(500, 5000)); // Starting balance from initial deposit
        var date = meta.CreatedAt;

        // Opening deposit
        txns.Add(MakeTxn(meta.AccountId, balance, "Opening Deposit", TransactionType.Deposit, balance, date));

        // Determine salary amount and frequency (fortnightly)
        var salary = RoundToNearest(rng.Next(1800, 6501), 50);
        var nextPayDay = date.AddDays(rng.Next(1, 15)); // First payday within 2 weeks
        // Pick 2-4 subscriptions for this customer
        var subCount = rng.Next(2, 5);
        var subs = new List<(string Desc, decimal Amount)>();
        var subIndices = new HashSet<int>();
        while (subIndices.Count < subCount && subIndices.Count < SubscriptionMerchants.Length)
            subIndices.Add(rng.Next(SubscriptionMerchants.Length));
        foreach (var idx in subIndices)
        {
            var m = SubscriptionMerchants[idx];
            subs.Add((m.Desc, RoundTo(RandomDecimal(rng, m.Min, m.Max), 0.01m)));
        }
        var nextSubDay = date.AddDays(rng.Next(1, 31));

        var currentDate = date.AddDays(1);
        while (currentDate < now)
        {
            // Salary (fortnightly)
            if (currentDate >= nextPayDay)
            {
                balance += salary;
                txns.Add(MakeTxn(meta.AccountId, salary, "Salary Credit",
                    TransactionType.Deposit, balance, nextPayDay));
                nextPayDay = nextPayDay.AddDays(14);
            }

            // Subscriptions (monthly)
            if (currentDate >= nextSubDay)
            {
                foreach (var (desc, amt) in subs)
                {
                    balance -= amt;
                    txns.Add(MakeTxn(meta.AccountId, -amt, desc,
                        TransactionType.Withdrawal, balance, nextSubDay.AddMinutes(rng.Next(0, 1440))));
                }
                nextSubDay = nextSubDay.AddMonths(1);
            }

            // Daily spending (0-3 transactions per day, weighted towards 1)
            var dailyTxnCount = rng.Next(100) switch
            {
                < 30 => 0,
                < 75 => 1,
                < 92 => 2,
                _ => 3
            };

            for (int t = 0; t < dailyTxnCount; t++)
            {
                var (desc, amount) = PickDebitTransaction(rng);
                balance -= amount;
                var txnTime = currentDate.AddHours(rng.Next(7, 22)).AddMinutes(rng.Next(0, 60));
                txns.Add(MakeTxn(meta.AccountId, -amount, desc,
                    TransactionType.Withdrawal, balance, txnTime));
            }

            // ATM withdrawal (~once every 2 weeks)
            if (rng.Next(14) == 0)
            {
                var atmAmount = (decimal)(rng.Next(1, 11) * 50); // $50-$500
                balance -= atmAmount;
                txns.Add(MakeTxn(meta.AccountId, -atmAmount, "ATM Withdrawal",
                    TransactionType.Withdrawal, balance,
                    currentDate.AddHours(rng.Next(8, 21))));
            }

            // Occasional transfer from savings (~weekly)
            if (rng.Next(7) == 0 && balance < 500)
            {
                var transfer = (decimal)(rng.Next(2, 11) * 100); // $200-$1000
                balance += transfer;
                txns.Add(MakeTxn(meta.AccountId, transfer, "Transfer from Savings",
                    TransactionType.Transfer, balance,
                    currentDate.AddHours(rng.Next(8, 18))));
            }

            currentDate = currentDate.AddDays(1);
        }

        return txns;
    }

    private static List<Transaction> GenerateSavingsAccountTxns(Random rng, AccountMeta meta, DateTime now)
    {
        var txns = new List<Transaction>();
        var balance = (decimal)(rng.Next(2000, 20001));
        var date = meta.CreatedAt;

        txns.Add(MakeTxn(meta.AccountId, balance, "Opening Deposit", TransactionType.Deposit, balance, date));

        var currentDate = date.AddDays(1);
        var nextInterestDate = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1); // First of next month

        while (currentDate < now)
        {
            // Monthly interest
            if (currentDate >= nextInterestDate)
            {
                var rate = meta.InterestRate ?? 3.5m;
                var interest = Math.Round(balance * rate / 100m / 12m, 2);
                if (interest > 0 && balance > 0)
                {
                    balance += interest;
                    txns.Add(MakeTxn(meta.AccountId, interest, "Interest Earned",
                        TransactionType.Interest, balance, nextInterestDate.AddHours(1)));
                }
                nextInterestDate = nextInterestDate.AddMonths(1);
            }

            // Transfer in (~2 per month)
            if (rng.Next(15) == 0)
            {
                var amount = (decimal)(rng.Next(2, 21) * 100); // $200-$2000
                balance += amount;
                txns.Add(MakeTxn(meta.AccountId, amount, "Transfer In",
                    TransactionType.Transfer, balance,
                    currentDate.AddHours(rng.Next(8, 18))));
            }

            // Transfer out (~1 per month)
            if (rng.Next(30) == 0 && balance > 1000)
            {
                var amount = (decimal)(rng.Next(1, 11) * 100); // $100-$1000
                balance -= amount;
                txns.Add(MakeTxn(meta.AccountId, -amount, "Transfer Out",
                    TransactionType.Transfer, balance,
                    currentDate.AddHours(rng.Next(8, 18))));
            }

            currentDate = currentDate.AddDays(1);
        }

        return txns;
    }

    private static List<Transaction> GenerateHomeLoanTxns(Random rng, AccountMeta meta, DateTime now)
    {
        var txns = new List<Transaction>();
        var loanAmount = meta.LoanAmount ?? 400000m;
        var rate = meta.InterestRate ?? 5.5m;
        var balance = -loanAmount; // Loans are negative

        txns.Add(MakeTxn(meta.AccountId, -loanAmount, "Loan Drawdown",
            TransactionType.Deposit, balance, meta.CreatedAt));

        // Calculate monthly repayment (P&I)
        var monthlyRate = rate / 100m / 12m;
        var termMonths = meta.LoanTermMonths ?? 360;
        var monthlyRepayment = loanAmount * monthlyRate *
            (decimal)Math.Pow((double)(1 + monthlyRate), termMonths) /
            ((decimal)Math.Pow((double)(1 + monthlyRate), termMonths) - 1);
        monthlyRepayment = Math.Round(monthlyRepayment, 2);

        var currentDate = meta.CreatedAt.AddDays(1);
        var nextInterestDate = new DateTime(meta.CreatedAt.Year, meta.CreatedAt.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);

        while (currentDate < now)
        {
            if (currentDate >= nextInterestDate)
            {
                // Monthly interest charge
                var interest = Math.Round(Math.Abs(balance) * rate / 100m / 12m, 2);
                balance -= interest;
                txns.Add(MakeTxn(meta.AccountId, -interest, "Interest Charged",
                    TransactionType.Interest, balance, nextInterestDate.AddHours(1)));

                // Monthly repayment (2 days after interest)
                var repayDate = nextInterestDate.AddDays(2);
                if (repayDate < now)
                {
                    balance += monthlyRepayment;
                    txns.Add(MakeTxn(meta.AccountId, monthlyRepayment, "Monthly Repayment",
                        TransactionType.Repayment, balance, repayDate));

                    // Occasional extra repayment (~10% of months)
                    if (rng.Next(10) == 0)
                    {
                        var extra = (decimal)(rng.Next(5, 31) * 100); // $500-$3000
                        balance += extra;
                        txns.Add(MakeTxn(meta.AccountId, extra, "Extra Repayment",
                            TransactionType.Repayment, balance, repayDate.AddDays(rng.Next(1, 10))));
                    }
                }

                nextInterestDate = nextInterestDate.AddMonths(1);
            }

            currentDate = currentDate.AddDays(1);
        }

        return txns;
    }

    private static List<Transaction> GenerateOffsetAccountTxns(Random rng, AccountMeta meta, DateTime now)
    {
        var txns = new List<Transaction>();
        var balance = (decimal)(rng.Next(5000, 50001));
        var date = meta.CreatedAt;

        txns.Add(MakeTxn(meta.AccountId, balance, "Opening Deposit", TransactionType.Deposit, balance, date));

        var salary = RoundToNearest(rng.Next(2500, 8001), 50);
        var nextPayDay = date.AddDays(rng.Next(1, 15));

        var currentDate = date.AddDays(1);
        while (currentDate < now)
        {
            // Salary (fortnightly)
            if (currentDate >= nextPayDay)
            {
                balance += salary;
                txns.Add(MakeTxn(meta.AccountId, salary, "Salary Credit",
                    TransactionType.Deposit, balance, nextPayDay));
                nextPayDay = nextPayDay.AddDays(14);
            }

            // Bills (~2 per month, larger amounts)
            if (rng.Next(15) == 0)
            {
                var bill = OffsetBills[rng.Next(OffsetBills.Length)];
                var amount = RoundTo(RandomDecimal(rng, bill.Min, bill.Max), 0.01m);
                balance -= amount;
                txns.Add(MakeTxn(meta.AccountId, -amount, bill.Desc,
                    TransactionType.Withdrawal, balance,
                    currentDate.AddHours(rng.Next(8, 18))));
            }

            // Transfer out (~weekly)
            if (rng.Next(7) == 0)
            {
                var amount = (decimal)(rng.Next(2, 16) * 100); // $200-$1500
                balance -= amount;
                txns.Add(MakeTxn(meta.AccountId, -amount, "Transfer Out",
                    TransactionType.Transfer, balance,
                    currentDate.AddHours(rng.Next(8, 18))));
            }

            // Transfer in (~every 10 days)
            if (rng.Next(10) == 0)
            {
                var amount = (decimal)(rng.Next(3, 21) * 100); // $300-$2000
                balance += amount;
                txns.Add(MakeTxn(meta.AccountId, amount, "Transfer In",
                    TransactionType.Transfer, balance,
                    currentDate.AddHours(rng.Next(8, 18))));
            }

            currentDate = currentDate.AddDays(1);
        }

        return txns;
    }

    private static (string Description, decimal Amount) PickDebitTransaction(Random rng)
    {
        // Pick category by weight
        var roll = rng.Next(100);
        var cumulative = 0;
        (string Desc, decimal Min, decimal Max)[] merchants = SupermarketMerchants;
        foreach (var (weight, m) in DebitCategories)
        {
            cumulative += weight;
            if (roll < cumulative)
            {
                merchants = m;
                break;
            }
        }

        var merchant = merchants[rng.Next(merchants.Length)];
        var amount = RoundTo(RandomDecimal(rng, merchant.Min, merchant.Max), 0.01m);
        return (merchant.Desc, amount);
    }

    private static Transaction MakeTxn(int accountId, decimal amount, string description,
        TransactionType type, decimal balanceAfter, DateTime createdAt)
    {
        return new Transaction
        {
            AccountId = accountId,
            Amount = amount,
            Description = description,
            TransactionType = type,
            BalanceAfter = Math.Round(balanceAfter, 2),
            CreatedAt = createdAt
        };
    }

    private static decimal RandomDecimal(Random rng, decimal min, decimal max)
    {
        return min + (decimal)rng.NextDouble() * (max - min);
    }

    private static decimal RoundTo(decimal value, decimal precision)
    {
        return Math.Round(value / precision) * precision;
    }

    private static decimal RoundToNearest(int value, int nearest)
    {
        return (decimal)(value / nearest * nearest);
    }

    private record AccountMeta(
        int AccountId, AccountType AccountType, DateTime CreatedAt,
        decimal? LoanAmount, decimal? InterestRate, int? LoanTermMonths);
}
