using BankOfGraeme.Api.Models;
using BankOfGraeme.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BankOfGraeme.Api.Data;

public static class SeedData
{
    private const int CustomerCount = 1_000;
    private const int RandomSeed = 42;
    private const string Bsb = "062-000";
    private const int CustomerBatchSize = 500;
    private const int TransactionBatchSize = 10_000;

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
        "Roman", "Anastasia", "Rafferty", "Georgie", "Lennox", "Asher", "Cohen", "Phoenix", "Elijah", "Ziggy"
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
        "Zhang", "Burns", "Huynh", "Tran", "Lam", "Kaur", "Sharma", "Doherty", "Lynch", "O'Connor"
    ];

    private static readonly string[] TransactionAccountNames =
        ["Everyday Transaction", "Smart Access", "Choice Account", "Everyday Account"];

    private static readonly string[] SavingsAccountNames =
        ["Goal Saver", "Bonus Saver", "Future Saver", "Rainy Day Fund"];

    private static readonly string[] HomeLoanNames =
        ["Home Loan", "Standard Variable Loan", "Fixed Rate Loan", "Investment Loan"];

    private static readonly string[] OffsetAccountNames =
        ["Offset Account", "Mortgage Offset", "Offset Saver"];

    private static readonly (int Weight, string[] Merchants, decimal Min, decimal Max)[] SpendingPools =
    [
        (24, ["WOOLWORTHS 0942 SYDNEY", "COLES 0871 NEWTOWN", "ALDI 0148 MARRICKVILLE", "IGA 352 LEICHHARDT", "HARRIS FARM MARKETS"], 8m, 260m),
        (9, ["BP ROZELLE", "SHELL REDFERN", "AMPOL PARRAMATTA", "7-ELEVEN FUEL STRATHFIELD"], 25m, 130m),
        (19, ["MCDONALD'S 317 SYDNEY", "GUZMAN Y GOMEZ SURRY HILLS", "SUSHI HUB TOWN HALL", "UBER EATS SYDNEY", "MENULOG PTY LTD"], 8m, 65m),
        (11, ["THE ROYAL HOTEL SYDNEY", "THE OXFORD TAVERN", "YULLI'S BREWPUB", "THE COURTHOUSE HOTEL"], 18m, 120m),
        (10, ["UBER *TRIP", "DIDI RIDE", "OPAL TOP UP", "WILSON PARKING", "SECURE PARKING"], 8m, 55m),
        (8, ["CHEMIST WAREHOUSE", "PRICELINE PHARMACY", "DENTAL CARE CENTRE", "DR SMITH MEDICAL"], 10m, 190m),
        (11, ["BUNNINGS 731", "JB HI-FI 214", "KMART 097", "BIG W 143", "TARGET 088"], 12m, 320m),
        (8, ["THE COFFEE BEAN", "GLORIA JEAN'S", "COFFEE CLUB", "BOOST JUICE"], 4m, 14m)
    ];

    private static readonly (string Payee, decimal Min, decimal Max, ScheduleFrequency Frequency)[] UtilityPayees =
    [
        ("AGL ENERGY", 95m, 340m, ScheduleFrequency.Monthly),
        ("ORIGIN ENERGY", 95m, 340m, ScheduleFrequency.Monthly),
        ("SYDNEY WATER", 70m, 210m, ScheduleFrequency.Quarterly),
        ("TELSTRA", 55m, 140m, ScheduleFrequency.Monthly),
        ("OPTUS", 45m, 120m, ScheduleFrequency.Monthly),
        ("VODAFONE", 40m, 100m, ScheduleFrequency.Monthly)
    ];

    private static readonly (string Payee, decimal Min, decimal Max)[] Streamers =
    [
        ("NETFLIX", 16.99m, 25.99m),
        ("DISNEY+", 13.99m, 17.99m),
        ("STAN", 12m, 21m),
        ("KAYO SPORTS", 27.99m, 35.99m),
        ("BINGE", 10m, 18m),
        ("SPOTIFY", 12.99m, 12.99m),
        ("YOUTUBE PREMIUM", 16.99m, 22.99m),
        ("AMAZON PRIME", 9.99m, 9.99m)
    ];

    private static readonly PersonaTemplate[] Personas =
    [
        new("Teen Starter", 7, 16, 19, 350m, 850m, IncomeFrequency.Weekly, HousingType.Dependent, false, 0.08, true, true, 0.08),
        new("Uni Student", 11, 18, 25, 650m, 1450m, IncomeFrequency.Fortnightly, HousingType.SharedRent, false, 0.18, true, true, 0.12),
        new("Early Worker", 14, 20, 29, 1400m, 2600m, IncomeFrequency.Fortnightly, HousingType.SharedRent, false, 0.22, true, true, 0.30),
        new("Young Professional", 15, 24, 36, 2400m, 4700m, IncomeFrequency.Fortnightly, HousingType.Renting, false, 0.26, true, true, 0.36),
        new("Family Renter", 10, 28, 45, 2600m, 5200m, IncomeFrequency.Fortnightly, HousingType.Renting, false, 0.24, true, false, 0.25),
        new("Mortgage Family", 14, 30, 55, 3200m, 6200m, IncomeFrequency.Fortnightly, HousingType.Mortgage, true, 0.22, true, false, 0.22),
        new("Affluent Professional", 7, 33, 52, 5200m, 9800m, IncomeFrequency.Monthly, HousingType.Mortgage, true, 0.30, true, false, 0.30),
        new("Small Business Operator", 5, 32, 60, 2800m, 8500m, IncomeFrequency.Weekly, HousingType.Mortgage, true, 0.28, true, false, 0.34),
        new("Financially Stretched", 10, 25, 55, 1200m, 2200m, IncomeFrequency.Fortnightly, HousingType.Renting, false, 0.17, false, true, 0.42),
        new("Pensioner Retiree", 7, 66, 83, 1700m, 3200m, IncomeFrequency.Fortnightly, HousingType.OwnedOutright, false, 0.14, false, false, 0.18)
    ];

    private static readonly SpotlightCustomer[] SpotlightCustomers =
    [
        new("Lily", "Nguyen", "Teen Starter", 16, 45),
        new("Noah", "Patel", "Uni Student", 21, 240),
        new("Chloe", "Martin", "Early Worker", 27, 365),
        new("Ethan", "Ross", "Young Professional", 33, 730),
        new("Grace", "Turner", "Family Renter", 39, 1095),
        new("Jack", "O'Connor", "Mortgage Family", 45, 1280),
        new("Amelia", "Chen", "Affluent Professional", 41, 980),
        new("Marcus", "Kelly", "Small Business Operator", 52, 540),
        new("Zoe", "Adams", "Financially Stretched", 34, 35),
        new("Gabriel", "White", "Pensioner Retiree", 74, 1680)
    ];

    public static void Seed(BankDbContext db, IDateTimeProvider dateTime)
    {
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
        var allCustomers = new List<Customer>(CustomerCount);
        var profiles = new List<CustomerProfile>(CustomerCount);

        for (int i = 0; i < CustomerCount; i++)
        {
            var profile = CreateCustomerProfile(rng, i, now);
            allCustomers.Add(profile.Customer);
            profiles.Add(profile);
        }

        foreach (var batch in allCustomers.Chunk(CustomerBatchSize))
        {
            db.Customers.AddRange(batch);
            db.SaveChanges();
            db.ChangeTracker.Clear();
        }

        var customerIds = db.Customers.Select(c => c.Id).OrderBy(id => id).ToList();
        var customerProfilesById = new Dictionary<int, CustomerProfile>(CustomerCount);
        for (int i = 0; i < customerIds.Count; i++)
            customerProfilesById[customerIds[i]] = profiles[i];

        var accountRng = new Random(RandomSeed + 1);
        var allAccounts = new List<Account>();
        var offsetLinks = new List<(int OffsetIndex, int HomeLoanIndex)>();
        int accountIndex = 0;

        foreach (var customerId in customerIds)
        {
            var profile = customerProfilesById[customerId];
            var customerAccounts = GenerateCustomerAccounts(
                accountRng, customerId, profile, now, ref accountIndex);

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

        foreach (var batch in allAccounts.Chunk(CustomerBatchSize))
        {
            db.Accounts.AddRange(batch);
            db.SaveChanges();
            db.ChangeTracker.Clear();
        }

        var accountIds = db.Accounts.OrderBy(a => a.Id).Select(a => new { a.Id, a.AccountType }).ToList();
        foreach (var (offsetIdx, homeLoanIdx) in offsetLinks)
        {
            var offset = db.Accounts.Find(accountIds[offsetIdx].Id)!;
            offset.HomeLoanAccountId = accountIds[homeLoanIdx].Id;
        }

        db.SaveChanges();
        db.ChangeTracker.Clear();

        var txnRng = new Random(RandomSeed + 2);
        var txnBuffer = new List<Transaction>(TransactionBatchSize);
        var scheduledSeeds = new List<ScheduledPaymentSeed>();

        var accountMetas = db.Accounts
            .OrderBy(a => a.Id)
            .Select(a => new AccountMeta(
                a.Id,
                a.CustomerId,
                a.AccountType,
                a.CreatedAt,
                a.LoanAmount,
                a.InterestRate,
                a.LoanTermMonths))
            .ToList();

        var homeLoanRepaymentByCustomer = accountMetas
            .Where(a => a.AccountType == AccountType.HomeLoan)
            .ToDictionary(
                a => a.CustomerId,
                a => CalculateMonthlyRepayment(
                    a.LoanAmount ?? 450000m,
                    a.InterestRate ?? 6.0m,
                    a.LoanTermMonths ?? 360));

        var offsetEstimateByCustomer = accountMetas
            .Where(a => a.AccountType == AccountType.Offset)
            .ToDictionary(
                a => a.CustomerId,
                a => EstimateOffsetBalanceForInterest(customerProfilesById[a.CustomerId]));

        foreach (var meta in accountMetas)
        {
            var profile = customerProfilesById[meta.CustomerId];
            homeLoanRepaymentByCustomer.TryGetValue(meta.CustomerId, out var homeLoanRepayment);
            offsetEstimateByCustomer.TryGetValue(meta.CustomerId, out var offsetEstimate);
            var (transactions, scheduleItems) = GenerateTransactions(
                txnRng,
                meta,
                profile,
                now,
                homeLoanRepayment,
                offsetEstimate);
            txnBuffer.AddRange(transactions);
            scheduledSeeds.AddRange(scheduleItems);

            if (txnBuffer.Count >= TransactionBatchSize)
                FlushTransactions(db, txnBuffer);
        }

        if (txnBuffer.Count > 0)
            FlushTransactions(db, txnBuffer);

        GenerateScheduledPayments(db, scheduledSeeds, now);
        UpdateAccountBalances(db);
        MarkRecentWithdrawalsAsPending(db, now);
        GenerateBalanceSnapshots(db);
        SetLastProcessedDate(db, now);
    }

    private static CustomerProfile CreateCustomerProfile(Random rng, int index, DateTime now)
    {
        var spotlight = index < SpotlightCustomers.Length ? SpotlightCustomers[index] : null;
        var persona = spotlight is not null
            ? Personas.First(p => p.LifeStage == spotlight.LifeStage)
            : PickPersona(rng);

        var age = spotlight?.Age ?? rng.Next(persona.MinAge, persona.MaxAge + 1);
        var dobYear = now.Year - age;
        var dobMonth = rng.Next(1, 13);
        var dobDay = rng.Next(1, DateTime.DaysInMonth(dobYear, dobMonth) + 1);

        var firstName = spotlight?.FirstName ?? FirstNames[rng.Next(FirstNames.Length)];
        var lastName = spotlight?.LastName ?? LastNames[rng.Next(LastNames.Length)];

        var salary = RoundTo(RandomDecimal(rng, persona.IncomeMin, persona.IncomeMax), 50m);
        var tenureDays = spotlight?.TenureDays ?? PickTenureDays(rng, persona.LifeStage);
        var binge = rng.NextDouble() < 0.35;
        var bingeService = binge ? Streamers[rng.Next(Streamers.Length)].Payee : null;
        var bingeMonths = binge ? rng.Next(2, 5) : 0;
        var bingeStartOffset = binge ? rng.Next(25, Math.Max(30, tenureDays - 40)) : 0;

        var customer = new Customer
        {
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName.ToLower()}.{lastName.ToLower()}{index}@email.com.au",
            Phone = $"04{rng.Next(10, 100):D2} {rng.Next(100, 1000):D3} {rng.Next(100, 1000):D3}",
            DateOfBirth = new DateOnly(dobYear, dobMonth, dobDay),
            CreatedAt = now.AddDays(-tenureDays)
        };

        return new CustomerProfile(
            customer,
            persona,
            salary,
            tenureDays,
            bingeService,
            bingeStartOffset,
            bingeMonths);
    }

    private static PersonaTemplate PickPersona(Random rng)
    {
        var roll = rng.Next(100);
        var cumulative = 0;
        foreach (var persona in Personas)
        {
            cumulative += persona.Weight;
            if (roll < cumulative)
                return persona;
        }

        return Personas[^1];
    }

    private static int PickTenureDays(Random rng, string lifeStage)
    {
        if (lifeStage == "Pensioner Retiree")
        {
            var retireeRoll = rng.Next(100);
            if (retireeRoll < 25) return rng.Next(1095, 1461);  // 3-4 years
            if (retireeRoll < 75) return rng.Next(1461, 2200);  // 4-6 years
            return rng.Next(2200, 3000);                        // 6-8+ years
        }

        var roll = rng.Next(100);
        if (roll < 20) return rng.Next(30, 91);       // 1-3 months
        if (roll < 58) return rng.Next(91, 730);      // 3-24 months
        if (roll < 88) return rng.Next(730, 1461);    // 2-4 years
        return rng.Next(1095, 1461);                  // 3-4 years
    }

    private static List<Account> GenerateCustomerAccounts(
        Random rng,
        int customerId,
        CustomerProfile profile,
        DateTime now,
        ref int accountIndex)
    {
        var accounts = new List<Account>();
        var accountAgeDays = profile.TenureDays;

        accounts.Add(CreateAccount(
            rng, customerId, AccountType.Transaction, accountAgeDays, now, ref accountIndex));

        var wantsSavings = rng.NextDouble() < profile.Persona.SavingsLikelihood;
        var hasHomeLoan = profile.Persona.DefaultHousing == HousingType.Mortgage || rng.NextDouble() < 0.15;

        if (hasHomeLoan)
        {
            accounts.Add(CreateAccount(
                rng, customerId, AccountType.HomeLoan, accountAgeDays, now, ref accountIndex));

            if (rng.NextDouble() < 0.85)
            {
                accounts.Add(CreateAccount(
                    rng, customerId, AccountType.Offset, accountAgeDays, now, ref accountIndex));
            }
        }

        if (wantsSavings || !hasHomeLoan || accounts.Count < 2)
        {
            accounts.Add(CreateAccount(
                rng, customerId, AccountType.Savings, accountAgeDays, now, ref accountIndex));
        }

        return accounts;
    }

    private static Account CreateAccount(
        Random rng,
        int customerId,
        AccountType type,
        int ageDays,
        DateTime now,
        ref int accountIndex)
    {
        var name = type switch
        {
            AccountType.Transaction => TransactionAccountNames[rng.Next(TransactionAccountNames.Length)],
            AccountType.Savings => SavingsAccountNames[rng.Next(SavingsAccountNames.Length)],
            AccountType.HomeLoan => HomeLoanNames[rng.Next(HomeLoanNames.Length)],
            AccountType.Offset => OffsetAccountNames[rng.Next(OffsetAccountNames.Length)],
            _ => "Account"
        };

        var account = new Account
        {
            CustomerId = customerId,
            AccountType = type,
            Bsb = Bsb,
            AccountNumber = $"{accountIndex:D8}",
            Name = name,
            Balance = 0m,
            CreatedAt = now.AddDays(-Math.Max(30, ageDays - rng.Next(0, 60)))
        };
        accountIndex++;

        if (type == AccountType.HomeLoan)
        {
            account.LoanAmount = (decimal)(rng.Next(320, 1201) * 1000); // $320k-$1.2m
            account.InterestRate = 4.9m + (decimal)rng.Next(0, 33) / 10m; // 4.9%-8.1%
            account.LoanTermMonths = rng.Next(100) < 75 ? 360 : 300;
        }

        if (type == AccountType.Savings)
        {
            account.InterestRate = 2.2m + (decimal)rng.Next(0, 30) / 10m;
        }

        return account;
    }

    private static (List<Transaction> Txns, List<ScheduledPaymentSeed> Schedules) GenerateTransactions(
        Random rng,
        AccountMeta meta,
        CustomerProfile profile,
        DateTime now,
        decimal homeLoanRepayment,
        decimal offsetEstimate)
    {
        return meta.AccountType switch
        {
            AccountType.Transaction => GenerateTransactionAccountTxns(rng, meta, profile, now, homeLoanRepayment),
            AccountType.Savings => (GenerateSavingsAccountTxns(rng, meta, profile, now), []),
            AccountType.HomeLoan => (GenerateHomeLoanTxns(rng, meta, now, offsetEstimate), []),
            AccountType.Offset => (GenerateOffsetAccountTxns(rng, meta, profile, now), []),
            _ => ([], [])
        };
    }

    private static (List<Transaction> Txns, List<ScheduledPaymentSeed> Schedules) GenerateTransactionAccountTxns(
        Random rng,
        AccountMeta meta,
        CustomerProfile profile,
        DateTime now,
        decimal homeLoanRepayment)
    {
        var txns = new List<Transaction>();
        var schedules = new List<ScheduledPaymentSeed>();

        var openingBalance = profile.Persona.LifeStage switch
        {
            "Affluent Professional" => RoundTo(RandomDecimal(rng, 3000m, 18000m), 50m),
            "Pensioner Retiree" => RoundTo(RandomDecimal(rng, 2500m, 12000m), 50m),
            "Financially Stretched" => RoundTo(RandomDecimal(rng, 100m, 900m), 50m),
            _ => RoundTo(RandomDecimal(rng, 450m, 4500m), 50m)
        };

        var balance = openingBalance;
        txns.Add(MakeTxn(meta.AccountId, openingBalance, "OPENING DEPOSIT", TransactionType.Deposit, meta.CreatedAt));

        var nextIncomeDate = meta.CreatedAt.AddDays(rng.Next(2, 10));
        var recurringPayments = BuildRecurringPayments(rng, profile, meta.CreatedAt, homeLoanRepayment);
        foreach (var recurring in recurringPayments.Where(r => r.IncludeScheduledPayment))
        {
            schedules.Add(new ScheduledPaymentSeed(
                meta.AccountId,
                recurring.PayeeName,
                recurring.Amount,
                recurring.Description,
                recurring.Frequency,
                recurring.FirstDate));
        }

        var currentDate = meta.CreatedAt.AddDays(1);
        while (currentDate < now)
        {
            if (currentDate >= nextIncomeDate)
            {
                var incomeDesc = profile.Persona.LifeStage == "Pensioner Retiree"
                    ? "SERVICES AUSTRALIA PENSION"
                    : profile.Persona.LifeStage == "Small Business Operator"
                        ? "BUSINESS RECEIPTS TRANSFER"
                        : "SALARY CREDIT";

                balance += profile.IncomeAmount;
                txns.Add(MakeTxn(meta.AccountId, profile.IncomeAmount, incomeDesc, TransactionType.Deposit, nextIncomeDate));
                nextIncomeDate = AdvanceByIncomeFrequency(nextIncomeDate, profile.Persona.IncomeFrequency);
            }

            foreach (var recurring in recurringPayments)
            {
                if (IsRecurringDue(recurring, currentDate))
                {
                    balance -= recurring.Amount;
                    txns.Add(MakeTxn(
                        meta.AccountId,
                        -recurring.Amount,
                        recurring.Description,
                        TransactionType.DirectDebit,
                        StampAtNineAm(currentDate, rng)));
                }
            }

            if (profile.BingeService is not null && IsBingeMonth(profile, currentDate, meta.CreatedAt))
            {
                var bingeCharge = RoundTo(RandomDecimal(rng, 12m, 29m), 0.01m);
                balance -= bingeCharge;
                txns.Add(MakeTxn(
                    meta.AccountId,
                    -bingeCharge,
                    $"CARD PURCHASE {profile.BingeService} AU",
                    TransactionType.Withdrawal,
                    currentDate.AddHours(20).AddMinutes(rng.Next(0, 60))));
            }

            var dailyTxns = profile.Persona.LifeStage switch
            {
                "Teen Starter" => rng.Next(100) < 60 ? 0 : 1,
                "Pensioner Retiree" => rng.Next(100) < 35 ? 0 : 1,
                _ => rng.Next(100) switch
                {
                    < 20 => 0,
                    < 70 => 1,
                    < 92 => 2,
                    _ => 3
                }
            };

            for (int i = 0; i < dailyTxns; i++)
            {
                var (desc, amount) = PickDebitTransaction(rng, profile.Persona.PubSpendLikelihood);
                balance -= amount;
                txns.Add(MakeTxn(
                    meta.AccountId,
                    -amount,
                    desc,
                    TransactionType.Withdrawal,
                    currentDate.AddHours(rng.Next(7, 23)).AddMinutes(rng.Next(0, 60))));
            }

            if (rng.Next(17) == 0)
            {
                var atmAmount = (decimal)(rng.Next(1, 9) * 50);
                balance -= atmAmount;
                txns.Add(MakeTxn(
                    meta.AccountId,
                    -atmAmount,
                    "ATM WITHDRAWAL",
                    TransactionType.Withdrawal,
                    currentDate.AddHours(rng.Next(8, 22))));
            }

            if (balance < 120 && rng.Next(5) == 0)
            {
                var rescue = (decimal)(rng.Next(2, 11) * 100);
                balance += rescue;
                txns.Add(MakeTxn(
                    meta.AccountId,
                    rescue,
                    "TRANSFER FROM SAVINGS",
                    TransactionType.Transfer,
                    currentDate.AddHours(rng.Next(8, 19))));
            }

            currentDate = currentDate.AddDays(1);
        }

        return (txns, schedules);
    }

    private static List<RecurringPaymentSeed> BuildRecurringPayments(
        Random rng,
        CustomerProfile profile,
        DateTime accountStart,
        decimal homeLoanRepayment)
    {
        var recurring = new List<RecurringPaymentSeed>();
        var firstMonth = accountStart.AddDays(rng.Next(1, 25));

        if (profile.Persona.DefaultHousing is HousingType.Renting or HousingType.SharedRent)
        {
            var rent = profile.Persona.DefaultHousing == HousingType.SharedRent
                ? RoundTo(RandomDecimal(rng, 180m, 430m), 1m)
                : RoundTo(RandomDecimal(rng, 520m, 1100m), 1m);

            recurring.Add(new RecurringPaymentSeed(
                "PROPERTY RENT",
                rent,
                "OSKO PAYMENT - RENT",
                ScheduleFrequency.Weekly,
                firstMonth,
                true));
        }

        if (profile.Persona.HasMortgage && homeLoanRepayment > 0)
        {
            recurring.Add(new RecurringPaymentSeed(
                "HOME LOAN REPAYMENT",
                homeLoanRepayment,
                "DIRECT DEBIT - HOME LOAN REPAYMENT",
                ScheduleFrequency.Monthly,
                firstMonth.AddDays(2),
                true));
        }

        if (profile.Persona.HasCoreUtilities)
        {
            var utility = UtilityPayees[rng.Next(UtilityPayees.Length)];
            recurring.Add(new RecurringPaymentSeed(
                utility.Payee,
                RoundTo(RandomDecimal(rng, utility.Min, utility.Max), 0.01m),
                $"DIRECT DEBIT - {utility.Payee}",
                utility.Frequency,
                firstMonth.AddDays(3),
                true));
        }

        var streamCount = profile.Persona.HasSubscriptions ? rng.Next(1, 4) : rng.Next(0, 2);
        var streamIndices = new HashSet<int>();
        while (streamIndices.Count < streamCount)
            streamIndices.Add(rng.Next(Streamers.Length));

        foreach (var index in streamIndices)
        {
            var streamer = Streamers[index];
            recurring.Add(new RecurringPaymentSeed(
                streamer.Payee,
                RoundTo(RandomDecimal(rng, streamer.Min, streamer.Max), 0.01m),
                $"DIRECT DEBIT - {streamer.Payee}",
                ScheduleFrequency.Monthly,
                firstMonth.AddDays(rng.Next(0, 10)),
                true));
        }

        return recurring;
    }

    private static bool IsRecurringDue(RecurringPaymentSeed recurring, DateTime date)
    {
        if (date < recurring.FirstDate.Date) return false;

        return recurring.Frequency switch
        {
            ScheduleFrequency.Weekly => (date.Date - recurring.FirstDate.Date).Days % 7 == 0,
            ScheduleFrequency.Fortnightly => (date.Date - recurring.FirstDate.Date).Days % 14 == 0,
            ScheduleFrequency.Monthly or ScheduleFrequency.Quarterly or ScheduleFrequency.Yearly => IsCalendarDue(recurring, date),
            _ => false
        };
    }

    private static bool IsCalendarDue(RecurringPaymentSeed recurring, DateTime date)
    {
        var target = DateOnly.FromDateTime(date);
        var due = DateOnly.FromDateTime(recurring.FirstDate);
        while (due < target)
            due = AdvanceNextDue(due, recurring.Frequency);

        return due == target;
    }

    private static bool IsBingeMonth(CustomerProfile profile, DateTime date, DateTime accountStart)
    {
        if (profile.BingeService is null || profile.BingeMonths <= 0) return false;
        var bingeStart = accountStart.AddDays(profile.BingeStartOffset).Date;
        var bingeEnd = bingeStart.AddMonths(profile.BingeMonths);
        return date.Date >= bingeStart && date.Date < bingeEnd && date.Day == Math.Min(28, bingeStart.Day);
    }

    private static List<Transaction> GenerateSavingsAccountTxns(Random rng, AccountMeta meta, CustomerProfile profile, DateTime now)
    {
        var txns = new List<Transaction>();
        var opening = profile.Persona.LifeStage switch
        {
            "Affluent Professional" => RoundTo(RandomDecimal(rng, 30000m, 120000m), 10m),
            "Pensioner Retiree" => RoundTo(RandomDecimal(rng, 18000m, 90000m), 10m),
            "Financially Stretched" => RoundTo(RandomDecimal(rng, 200m, 2200m), 10m),
            _ => RoundTo(RandomDecimal(rng, 3000m, 30000m), 10m)
        };

        var balance = opening;
        txns.Add(MakeTxn(meta.AccountId, opening, "OPENING DEPOSIT", TransactionType.Deposit, meta.CreatedAt));

        var currentDate = meta.CreatedAt.AddDays(1);
        var nextInterestDate = new DateTime(meta.CreatedAt.Year, meta.CreatedAt.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);

        while (currentDate < now)
        {
            if (currentDate >= nextInterestDate)
            {
                var rate = meta.InterestRate ?? 3.5m;
                var interest = Math.Round(balance * rate / 100m / 12m, 2);
                if (interest > 0)
                {
                    balance += interest;
                    txns.Add(MakeTxn(meta.AccountId, interest, "INTEREST PAID", TransactionType.Interest, nextInterestDate.AddHours(1)));
                }
                nextInterestDate = nextInterestDate.AddMonths(1);
            }

            if (rng.Next(14) == 0)
            {
                var inAmount = RoundTo(RandomDecimal(rng, 150m, 2000m), 1m);
                balance += inAmount;
                txns.Add(MakeTxn(meta.AccountId, inAmount, "TRANSFER IN", TransactionType.Transfer, currentDate.AddHours(rng.Next(8, 19))));
            }

            if (rng.Next(28) == 0 && balance > 1000)
            {
                var outAmount = RoundTo(RandomDecimal(rng, 120m, 1500m), 1m);
                balance -= outAmount;
                txns.Add(MakeTxn(meta.AccountId, -outAmount, "TRANSFER OUT", TransactionType.Transfer, currentDate.AddHours(rng.Next(8, 19))));
            }

            currentDate = currentDate.AddDays(1);
        }

        return txns;
    }

    private static List<Transaction> GenerateHomeLoanTxns(Random rng, AccountMeta meta, DateTime now, decimal offsetEstimate)
    {
        var txns = new List<Transaction>();
        var loanAmount = meta.LoanAmount ?? 450000m;
        var rate = meta.InterestRate ?? 6.0m;
        var balance = -loanAmount;

        txns.Add(MakeTxn(meta.AccountId, -loanAmount, "LOAN DRAWDOWN", TransactionType.Deposit, meta.CreatedAt));

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
                var interestBase = Math.Max(0, Math.Abs(balance) - offsetEstimate);
                var interest = Math.Round(interestBase * rate / 100m / 12m, 2);
                balance -= interest;
                txns.Add(MakeTxn(meta.AccountId, -interest, "INTEREST CHARGED", TransactionType.Interest, nextInterestDate.AddHours(1)));

                var repaymentDate = nextInterestDate.AddDays(2);
                if (repaymentDate < now)
                {
                    balance += monthlyRepayment;
                    txns.Add(MakeTxn(meta.AccountId, monthlyRepayment, "MONTHLY REPAYMENT", TransactionType.Repayment, repaymentDate));

                    if (rng.Next(100) < 18)
                    {
                        var extra = RoundTo(RandomDecimal(rng, 300m, 2500m), 1m);
                        balance += extra;
                        txns.Add(MakeTxn(meta.AccountId, extra, "EXTRA REPAYMENT", TransactionType.Repayment, repaymentDate.AddDays(rng.Next(1, 10))));
                    }
                }

                nextInterestDate = nextInterestDate.AddMonths(1);
            }

            currentDate = currentDate.AddDays(1);
        }

        return txns;
    }

    private static List<Transaction> GenerateOffsetAccountTxns(Random rng, AccountMeta meta, CustomerProfile profile, DateTime now)
    {
        var txns = new List<Transaction>();
        var opening = profile.Persona.LifeStage == "Affluent Professional"
            ? RoundTo(RandomDecimal(rng, 35000m, 220000m), 10m)
            : RoundTo(RandomDecimal(rng, 8000m, 85000m), 10m);

        var balance = opening;
        txns.Add(MakeTxn(meta.AccountId, opening, "OPENING DEPOSIT", TransactionType.Deposit, meta.CreatedAt));

        var currentDate = meta.CreatedAt.AddDays(1);

        while (currentDate < now)
        {
            if (rng.Next(10) == 0)
            {
                var outAmount = RoundTo(RandomDecimal(rng, 350m, 2100m), 1m);
                balance -= outAmount;
                txns.Add(MakeTxn(meta.AccountId, -outAmount, "TRANSFER OUT", TransactionType.Transfer, currentDate.AddHours(rng.Next(8, 19))));
            }

            if (rng.Next(14) == 0)
            {
                var inAmount = RoundTo(RandomDecimal(rng, 250m, 1600m), 1m);
                balance += inAmount;
                txns.Add(MakeTxn(meta.AccountId, inAmount, "TRANSFER IN", TransactionType.Transfer, currentDate.AddHours(rng.Next(8, 19))));
            }

            currentDate = currentDate.AddDays(1);
        }

        return txns;
    }

    private static DateTime AdvanceByIncomeFrequency(DateTime current, IncomeFrequency frequency)
    {
        return frequency switch
        {
            IncomeFrequency.Weekly => current.AddDays(7),
            IncomeFrequency.Monthly => current.AddMonths(1),
            _ => current.AddDays(14)
        };
    }

    private static decimal CalculateMonthlyRepayment(decimal loanAmount, decimal annualRate, int termMonths)
    {
        var monthlyRate = annualRate / 100m / 12m;
        var repayment = loanAmount * monthlyRate *
            (decimal)Math.Pow((double)(1 + monthlyRate), termMonths) /
            ((decimal)Math.Pow((double)(1 + monthlyRate), termMonths) - 1);
        return Math.Round(repayment, 2);
    }

    private static decimal EstimateOffsetBalanceForInterest(CustomerProfile profile)
    {
        return profile.Persona.LifeStage switch
        {
            "Affluent Professional" => RoundTo(profile.IncomeAmount * 18m, 10m),
            "Mortgage Family" => RoundTo(profile.IncomeAmount * 10m, 10m),
            "Small Business Operator" => RoundTo(profile.IncomeAmount * 12m, 10m),
            _ => RoundTo(profile.IncomeAmount * 8m, 10m)
        };
    }

    private static (string Description, decimal Amount) PickDebitTransaction(Random rng, double pubSpendLikelihood)
    {
        if (rng.NextDouble() < pubSpendLikelihood)
        {
            var pubPool = SpendingPools[3];
            var pubMerchant = pubPool.Merchants[rng.Next(pubPool.Merchants.Length)];
            var pubAmount = RoundTo(RandomDecimal(rng, pubPool.Min, pubPool.Max), 0.01m);
            return (pubMerchant, pubAmount);
        }

        var nonPubWeight = 0;
        for (int i = 0; i < SpendingPools.Length; i++)
        {
            if (i != 3)
                nonPubWeight += SpendingPools[i].Weight;
        }

        var roll = rng.Next(nonPubWeight);
        var cumulative = 0;

        for (int i = 0; i < SpendingPools.Length; i++)
        {
            if (i == 3) continue;
            var (weight, merchants, min, max) = SpendingPools[i];
            cumulative += weight;
            if (roll < cumulative)
            {
                var merchant = merchants[rng.Next(merchants.Length)];
                var amount = RoundTo(RandomDecimal(rng, min, max), 0.01m);
                return (merchant, amount);
            }
        }

        return ("COLES 0001", RoundTo(RandomDecimal(rng, 8m, 40m), 0.01m));
    }

    private static DateTime StampAtNineAm(DateTime date, Random rng) =>
        date.Date.AddHours(9).AddMinutes(rng.Next(0, 45));

    private static void GenerateScheduledPayments(BankDbContext db, List<ScheduledPaymentSeed> schedules, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);
        var scheduledPayments = new List<ScheduledPayment>();

        foreach (var seed in schedules.DistinctBy(s => $"{s.AccountId}|{s.PayeeName}|{s.Amount}|{s.Frequency}|{s.Description}"))
        {
            var startDate = DateOnly.FromDateTime(seed.FirstBillingDate);
            var nextDue = startDate;
            while (nextDue <= today)
                nextDue = AdvanceNextDue(nextDue, seed.Frequency);

            scheduledPayments.Add(new ScheduledPayment
            {
                AccountId = seed.AccountId,
                PayeeName = seed.PayeeName,
                Amount = seed.Amount,
                Description = seed.Description,
                Frequency = seed.Frequency,
                StartDate = startDate,
                NextDueDate = nextDue,
                IsActive = true
            });
        }

        foreach (var batch in scheduledPayments.Chunk(CustomerBatchSize))
        {
            db.ScheduledPayments.AddRange(batch);
            db.SaveChanges();
            db.ChangeTracker.Clear();
        }
    }

    private static DateOnly AdvanceNextDue(DateOnly current, ScheduleFrequency frequency)
    {
        return frequency switch
        {
            ScheduleFrequency.Weekly => current.AddDays(7),
            ScheduleFrequency.Fortnightly => current.AddDays(14),
            ScheduleFrequency.Monthly => current.AddMonths(1),
            ScheduleFrequency.Quarterly => current.AddMonths(3),
            ScheduleFrequency.Yearly => current.AddYears(1),
            _ => current.AddMonths(1)
        };
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
        db.Database.ExecuteSqlRaw("""
            UPDATE "Accounts" a
            SET "Balance" = COALESCE(t.total, 0)
            FROM (
                SELECT "AccountId", SUM("Amount") as total
                FROM "Transactions"
                WHERE "Status" = 'Settled'
                GROUP BY "AccountId"
            ) t
            WHERE a."Id" = t."AccountId"
        """);
    }

    private static void MarkRecentWithdrawalsAsPending(BankDbContext db, DateTime now)
    {
        var cutoff = now.AddDays(-3);
        db.Database.ExecuteSqlRaw("""
            WITH ranked AS (
                SELECT "Id",
                       ROW_NUMBER() OVER (PARTITION BY "AccountId" ORDER BY "CreatedAt" DESC) as rn
                FROM "Transactions"
                WHERE "TransactionType" = 1
                  AND "CreatedAt" >= {0}
                  AND "Status" = 'Settled'
                  AND "Amount" < 0
            )
            UPDATE "Transactions"
            SET "Status" = 'Pending', "SettledAt" = NULL
            FROM ranked
            WHERE "Transactions"."Id" = ranked."Id" AND ranked.rn <= 2
        """, cutoff);

        db.Database.ExecuteSqlRaw("""
            UPDATE "Accounts" a
            SET "Balance" = "Balance" - COALESCE(p.pending_total, 0)
            FROM (
                SELECT "AccountId", SUM("Amount") as pending_total
                FROM "Transactions"
                WHERE "Status" = 'Pending'
                GROUP BY "AccountId"
            ) p
            WHERE a."Id" = p."AccountId"
        """);
    }

    private static void GenerateBalanceSnapshots(BankDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            WITH daily_settled AS (
                SELECT "AccountId",
                       "CreatedAt"::date AS txn_date,
                       SUM("Amount") AS day_amount
                FROM "Transactions"
                WHERE "Status" = 'Settled'
                GROUP BY "AccountId", "CreatedAt"::date
            ),
            running AS (
                SELECT "AccountId",
                       txn_date,
                       SUM(day_amount) OVER (
                           PARTITION BY "AccountId" ORDER BY txn_date
                       ) AS ledger_balance
                FROM daily_settled
            )
            INSERT INTO "AccountBalanceSnapshots"
                ("AccountId", "SnapshotDate", "LedgerBalance", "AvailableBalance", "CreatedAt")
            SELECT r."AccountId",
                   r.txn_date,
                   r.ledger_balance,
                   r.ledger_balance + p.cumulative_pending,
                   (r.txn_date + TIME '23:59:59')::timestamp
            FROM running r
            LEFT JOIN LATERAL (
                SELECT COALESCE(SUM(t2."Amount"), 0) AS cumulative_pending
                FROM "Transactions" t2
                WHERE t2."Status" = 'Pending'
                  AND t2."Amount" < 0
                  AND t2."AccountId" = r."AccountId"
                  AND t2."CreatedAt"::date <= r.txn_date
            ) p ON true
        """);
    }

    private static void SetLastProcessedDate(BankDbContext db, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);
        db.SystemSettings.Add(new SystemSettings
        {
            Key = "LastProcessedDate",
            Value = today.ToString("yyyy-MM-dd")
        });
        db.SaveChanges();
    }

    private static Transaction MakeTxn(
        int accountId,
        decimal amount,
        string description,
        TransactionType type,
        DateTime createdAt)
    {
        return new Transaction
        {
            AccountId = accountId,
            Amount = amount,
            Description = description,
            TransactionType = type,
            Status = TransactionStatus.Settled,
            SettledAt = createdAt,
            CreatedAt = createdAt
        };
    }

    private static decimal RandomDecimal(Random rng, decimal min, decimal max) =>
        min + (decimal)rng.NextDouble() * (max - min);

    private static decimal RoundTo(decimal value, decimal precision) =>
        Math.Round(value / precision) * precision;

    private enum IncomeFrequency
    {
        Weekly,
        Fortnightly,
        Monthly
    }

    private enum HousingType
    {
        Dependent,
        SharedRent,
        Renting,
        Mortgage,
        OwnedOutright
    }

    private sealed record PersonaTemplate(
        string LifeStage,
        int Weight,
        int MinAge,
        int MaxAge,
        decimal IncomeMin,
        decimal IncomeMax,
        IncomeFrequency IncomeFrequency,
        HousingType DefaultHousing,
        bool HasMortgage,
        double SavingsLikelihood,
        bool HasCoreUtilities,
        bool HasSubscriptions,
        double PubSpendLikelihood);

    private sealed record SpotlightCustomer(
        string FirstName,
        string LastName,
        string LifeStage,
        int Age,
        int TenureDays);

    private sealed record CustomerProfile(
        Customer Customer,
        PersonaTemplate Persona,
        decimal IncomeAmount,
        int TenureDays,
        string? BingeService,
        int BingeStartOffset,
        int BingeMonths);

    private sealed record AccountMeta(
        int AccountId,
        int CustomerId,
        AccountType AccountType,
        DateTime CreatedAt,
        decimal? LoanAmount,
        decimal? InterestRate,
        int? LoanTermMonths);

    private sealed record RecurringPaymentSeed(
        string PayeeName,
        decimal Amount,
        string Description,
        ScheduleFrequency Frequency,
        DateTime FirstDate,
        bool IncludeScheduledPayment);

    private sealed record ScheduledPaymentSeed(
        int AccountId,
        string PayeeName,
        decimal Amount,
        string Description,
        ScheduleFrequency Frequency,
        DateTime FirstBillingDate);
}
