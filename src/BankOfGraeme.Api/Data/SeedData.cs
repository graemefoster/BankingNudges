using BankOfGraeme.Api.Models;
using BankOfGraeme.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BankOfGraeme.Api.Data;

public static class SeedData
{
    private const int CustomerCount = 150;
    private const int RandomSeed = 42;
    private const string Bsb = "062-000";
    private const int TransactionBatchSize = 5_000;
    private const decimal TransactionInterestRate = 3.00m;

    // Bank opened on this date - oldest accounts start here
    private static readonly DateTime BankOpeningDate = new(2023, 7, 2, 0, 0, 0, DateTimeKind.Utc);

    private static readonly string[] FirstNames =
    [
        "Oliver", "Charlotte", "Jack", "Amelia", "Noah", "Isla", "William", "Mia", "James", "Ava",
        "Thomas", "Grace", "Chloe", "Henry", "Olivia", "Ethan", "Sophie", "Alexander", "Emily",
        "Liam", "Harper", "Ella", "Sebastian", "Lily", "Zoe", "Ruby", "Leo", "Matilda",
        "Hudson", "Willow", "Ivy", "Sienna", "Hunter", "Aria", "Charlie", "Scarlett",
        "Lachlan", "Layla", "Oscar", "Piper", "Max", "Poppy", "Samuel", "Luna", "Ryan",
        "Hannah", "Finn", "Mackenzie", "George", "Ellie", "Kai", "Violet", "Nathan",
        "Jake", "Aurora", "Caleb", "Hazel", "Mitchell", "Stella", "Dylan", "Georgia",
        "Angus", "Mila", "Connor", "Daisy", "Joshua", "Penelope", "Flynn", "Jasmine",
        "Bailey", "Freya", "Levi", "Maya", "Owen", "Eden", "Xavier", "Phoebe", "Felix",
        "Clara", "Bodhi", "Thea", "Blake", "Imogen", "Beau", "Abigail", "Jesse", "Sage",
        "Aiden", "Olive", "Toby", "Quinn", "Dominic", "Nora", "Gabriel", "Florence",
        "Heidi", "Darcy", "Miles", "Emilia", "Heath", "Millie", "Roman", "Margaret", "Georgie"
    ];

    private static readonly string[] LastNames =
    [
        "Smith", "Jones", "Williams", "Brown", "Wilson", "Taylor", "Johnson", "White", "Martin", "Anderson",
        "Thompson", "Nguyen", "Thomas", "Walker", "Harris", "Lee", "Ryan", "Robinson", "Kelly", "King",
        "Chen", "Davis", "Wright", "Clark", "Hall", "Young", "Mitchell", "Green", "Campbell", "Edwards",
        "Turner", "Roberts", "Parker", "Evans", "Collins", "Murphy", "Morris", "Cook", "Rogers", "Morgan",
        "Cooper", "Richardson", "Watson", "Brooks", "Wood", "Stewart", "McDonald", "Singh",
        "Ward", "Reid", "Ross", "Bennett", "Gray", "Fraser", "Hamilton", "Murray", "Patel",
        "Hughes", "Bell", "Baker", "Shaw", "Ali", "Adams", "Chapman", "Grant", "Simpson", "Li",
        "Kennedy", "Palmer", "Gibson", "Webb", "Russell", "Sullivan", "Henderson", "Cole",
        "Hart", "Wang", "Fox", "Hunt", "Price", "Carter", "Bailey", "Burton",
        "Fisher", "Black", "Graham", "Pearce", "Dixon", "Stone", "Knight", "Burke", "Doyle",
        "Zhang", "Burns", "Huynh", "Tran", "Lam", "Kaur", "Sharma"
    ];

    private static readonly PersonaTemplate[] Personas =
    [
        new("Student", 12, 16, 22, 120m, 380m, IncomeFrequency.Weekly,
            RentRange: (280m, 450m), RentLikelihood: 0.3, CarInsuranceLikelihood: 0.15,
            CarInsuranceRange: (30m, 40m), CarInsuranceType: "3rd Party"),
        new("Zero-Hours Worker", 10, 18, 28, 150m, 520m, IncomeFrequency.Weekly,
            RentRange: (400m, 650m), RentLikelihood: 0.85, CarInsuranceLikelihood: 0.4,
            CarInsuranceRange: (35m, 55m), CarInsuranceType: "3rd Party"),
        new("Young Professional", 15, 25, 35, 2500m, 3500m, IncomeFrequency.Fortnightly,
            RentRange: (700m, 1100m), RentLikelihood: 0.95, CarInsuranceLikelihood: 0.7,
            CarInsuranceRange: (80m, 140m), CarInsuranceType: "Comprehensive"),
        new("Established Professional", 13, 35, 50, 3500m, 5500m, IncomeFrequency.Fortnightly,
            RentRange: (1000m, 1500m), RentLikelihood: 0.95, CarInsuranceLikelihood: 0.85,
            CarInsuranceRange: (100m, 180m), CarInsuranceType: "Comprehensive"),
        new("Young Family", 15, 28, 42, 3000m, 5500m, IncomeFrequency.Fortnightly,
            RentRange: (900m, 1400m), RentLikelihood: 0.95, CarInsuranceLikelihood: 0.9,
            CarInsuranceRange: (120m, 200m), CarInsuranceType: "Comprehensive"),
        new("Single Parent", 10, 28, 45, 2000m, 3200m, IncomeFrequency.Fortnightly,
            RentRange: (700m, 1100m), RentLikelihood: 0.95, CarInsuranceLikelihood: 0.6,
            CarInsuranceRange: (40m, 70m), CarInsuranceType: "3rd Party Fire & Theft"),
        new("Comfortable Retiree", 12, 65, 82, 2000m, 3500m, IncomeFrequency.Fortnightly,
            RentRange: (0m, 0m), RentLikelihood: 0.0, CarInsuranceLikelihood: 0.8,
            CarInsuranceRange: (70m, 120m), CarInsuranceType: "Comprehensive"),
        new("Modest Retiree", 13, 65, 85, 1200m, 1800m, IncomeFrequency.Fortnightly,
            RentRange: (0m, 0m), RentLikelihood: 0.0, CarInsuranceLikelihood: 0.5,
            CarInsuranceRange: (25m, 40m), CarInsuranceType: "3rd Party")
    ];

    private static readonly SpotlightCustomer[] SpotlightCustomers =
    [
        new("Lily", "Nguyen", "Student", 17, 60),
        new("Noah", "Patel", "Zero-Hours Worker", 22, 280),
        new("Chloe", "Martin", "Young Professional", 28, 400),
        new("Ethan", "Ross", "Established Professional", 42, 750),
        new("Grace", "Turner", "Young Family", 35, 900),
        new("Zoe", "Adams", "Single Parent", 34, 200),
        new("Gabriel", "White", "Comfortable Retiree", 72, 980),
        new("Margaret", "Kelly", "Modest Retiree", 78, 970)
    ];

    private static readonly string[] CasualEmployers =
    [
        "MCDONALDS", "WOOLWORTHS", "COLES", "KFC", "HUNGRY JACKS", "BUNNINGS",
        "TARGET", "KMART", "JB HI-FI", "THE COFFEE CLUB", "DOMINOS", "SUBWAY"
    ];

    private static readonly (string Name, decimal Min, decimal Max)[] GroceryMerchants =
    [
        ("WOOLWORTHS", 15m, 220m), ("COLES", 15m, 220m), ("ALDI", 12m, 160m), ("IGA", 8m, 80m)
    ];

    private static readonly (string Name, decimal Min, decimal Max)[] DiningMerchants =
    [
        ("MCDONALDS", 8m, 25m), ("UBER EATS", 15m, 45m), ("DOORDASH", 18m, 50m),
        ("MENULOG", 15m, 40m), ("GUZMAN Y GOMEZ", 12m, 28m), ("NANDOS", 15m, 35m),
        ("THE COFFEE BEAN", 4m, 8m), ("GRILLD", 16m, 35m)
    ];

    private static readonly (string Name, decimal Min, decimal Max)[] TransportMerchants =
    [
        ("OPAL TOP UP", 10m, 50m), ("UBER *TRIP", 8m, 45m),
        ("SHELL", 40m, 120m), ("BP", 40m, 120m), ("AMPOL", 40m, 120m),
        ("7-ELEVEN FUEL", 35m, 100m)
    ];

    private static readonly (string Name, decimal Min, decimal Max)[] HealthMerchants =
    [
        ("CHEMIST WAREHOUSE", 8m, 65m), ("PRICELINE PHARMACY", 10m, 55m),
        ("DR SMITH MEDICAL", 40m, 90m)
    ];

    private static readonly (string Name, decimal Min, decimal Max)[] RetailMerchants =
    [
        ("KMART", 8m, 80m), ("TARGET", 12m, 120m), ("BIG W", 10m, 90m),
        ("BUNNINGS", 15m, 250m), ("JB HI-FI", 20m, 300m), ("OFFICEWORKS", 10m, 80m),
        ("THE ICONIC", 25m, 150m), ("AMAZON AU", 10m, 200m)
    ];

    private static readonly (string Name, decimal Min, decimal Max)[] EntertainmentMerchants =
    [
        ("THE ROYAL HOTEL", 20m, 80m), ("THE OXFORD TAVERN", 15m, 65m),
        ("DAN MURPHYS", 20m, 80m), ("BWS", 12m, 50m),
        ("HOYTS CINEMAS", 18m, 40m), ("EVENT CINEMAS", 16m, 38m)
    ];

    private static readonly (string Name, decimal Amount)[] StreamingServices =
    [
        ("Stan", 12m), ("Netflix", 17m), ("Disney+", 14m), ("Spotify", 13m),
        ("YouTube Premium", 15m), ("Binge", 10m), ("Paramount+", 9m), ("Apple TV+", 13m)
    ];

    private static readonly (string Name, decimal Min, decimal Max)[] MobileProviders =
    [
        ("TELSTRA", 55m, 90m), ("OPTUS", 40m, 65m), ("VODAFONE", 30m, 55m)
    ];

    private static readonly (string Name, decimal Min, decimal Max)[] UtilityProviders =
    [
        ("AGL ENERGY", 180m, 450m), ("ORIGIN ENERGY", 180m, 450m), ("SYDNEY WATER", 120m, 280m)
    ];

    private static readonly string[] CarInsuranceProviders = ["NRMA", "AAMI", "ALLIANZ", "SUNCORP", "RACV"];

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
        var transactionBuffer = new List<Transaction>(TransactionBatchSize);
        var scheduledPayments = new List<ScheduledPaymentSeed>();
        int accountNumber = 10000001;
        int spotlightIndex = 0;

        for (int i = 0; i < CustomerCount; i++)
        {
            var profile = CreateCustomerProfile(rng, i, now, ref spotlightIndex);
            db.Customers.Add(profile.Customer);
            db.SaveChanges();

            var account = new Account
            {
                CustomerId = profile.Customer.Id,
                AccountType = AccountType.Transaction,
                Bsb = Bsb,
                AccountNumber = (accountNumber++).ToString(),
                Name = "Everyday Transaction",
                Balance = 0m,
                IsActive = true,
                InterestRate = TransactionInterestRate,
            };
            db.Accounts.Add(account);
            db.SaveChanges();

            var (txns, schedules) = GenerateTransactionHistory(rng, account, profile, now);

            foreach (var txn in txns)
            {
                transactionBuffer.Add(txn);
                if (transactionBuffer.Count >= TransactionBatchSize)
                    FlushTransactions(db, transactionBuffer);
            }
            scheduledPayments.AddRange(schedules);
        }

        FlushTransactions(db, transactionBuffer);
        UpdateAccountBalances(db);
        MarkRecentWithdrawalsAsPending(db, now);
        GenerateScheduledPayments(db, scheduledPayments, now);
        GenerateBalanceSnapshots(db);
        SetLastProcessedDate(db, now);
    }

    private static CustomerProfile CreateCustomerProfile(
        Random rng, int index, DateTime now, ref int spotlightIndex)
    {
        PersonaTemplate persona;
        string firstName;
        string lastName;
        int age;
        int tenureDays;

        if (spotlightIndex < SpotlightCustomers.Length)
        {
            var spotlight = SpotlightCustomers[spotlightIndex];
            persona = Personas.First(p => p.Name == spotlight.LifeStage);
            firstName = spotlight.FirstName;
            lastName = spotlight.LastName;
            age = spotlight.Age;
            tenureDays = spotlight.TenureDays;
            spotlightIndex++;
        }
        else
        {
            persona = PickPersona(rng);
            firstName = FirstNames[rng.Next(FirstNames.Length)];
            lastName = LastNames[rng.Next(LastNames.Length)];
            age = rng.Next(persona.MinAge, persona.MaxAge + 1);
            tenureDays = PickTenureDays(rng, persona.Name, now);
        }

        var dob = DateOnly.FromDateTime(now.AddYears(-age).AddDays(-rng.Next(365)));
        var createdAt = now.AddDays(-tenureDays);

        if (createdAt < BankOpeningDate)
            createdAt = BankOpeningDate;

        var customer = new Customer
        {
            FirstName = firstName,
            LastName = lastName,
            Email = string.Concat(firstName.ToLower(), ".", lastName.ToLower().Replace("'", ""), index, "@email.com.au"),
            Phone = string.Concat("04", rng.Next(10, 100).ToString("00"), " ", rng.Next(100, 1000).ToString("000"), " ", rng.Next(100, 1000).ToString("000")),
            DateOfBirth = dob,
            CreatedAt = createdAt,
            Persona = persona.Name,
        };

        var incomeAmount = RandomDecimal(rng, persona.IncomeMin, persona.IncomeMax);
        var incomeVariability = persona.Name == "Zero-Hours Worker" ? 0.4 : 0.05;

        // Assign employers for casual workers (1-2 jobs)
        var employers = new List<string>();
        if (persona.Name is "Student" or "Zero-Hours Worker")
        {
            employers.Add(CasualEmployers[rng.Next(CasualEmployers.Length)]);
            // ~20% chance of a second job
            if (rng.NextDouble() < 0.2)
            {
                string second;
                do { second = CasualEmployers[rng.Next(CasualEmployers.Length)]; }
                while (second == employers[0]);
                employers.Add(second);
            }
        }

        return new CustomerProfile(customer, persona, incomeAmount, tenureDays, incomeVariability, employers);
    }

    private static PersonaTemplate PickPersona(Random rng)
    {
        var totalWeight = Personas.Sum(p => p.Weight);
        var roll = rng.Next(totalWeight);
        var cumulative = 0;
        foreach (var p in Personas)
        {
            cumulative += p.Weight;
            if (roll < cumulative) return p;
        }
        return Personas[^1];
    }

    private static int PickTenureDays(Random rng, string persona, DateTime now)
    {
        var maxDays = Math.Max(14, (int)(now - BankOpeningDate).TotalDays);

        return persona switch
        {
            "Student" => rng.Next(7, Math.Min(365, maxDays)),
            "Zero-Hours Worker" => rng.Next(14, Math.Min(730, maxDays)),
            "Young Professional" => rng.Next(30, Math.Min(900, maxDays)),
            "Established Professional" => rng.Next(60, maxDays),
            "Young Family" => rng.Next(60, maxDays),
            "Single Parent" => rng.Next(14, Math.Min(730, maxDays)),
            "Comfortable Retiree" => rng.Next(180, maxDays),
            "Modest Retiree" => rng.Next(180, maxDays),
            _ => rng.Next(30, Math.Min(365, maxDays)),
        };
    }

    private static (List<Transaction> Txns, List<ScheduledPaymentSeed> Schedules)
        GenerateTransactionHistory(Random rng, Account account, CustomerProfile profile, DateTime now)
    {
        var txns = new List<Transaction>();
        var schedules = new List<ScheduledPaymentSeed>();
        var accountStart = profile.Customer.CreatedAt;
        var persona = profile.Persona;

        // Opening deposit
        var openingDeposit = persona.Name switch
        {
            "Student" => RandomDecimal(rng, 20m, 150m),
            "Zero-Hours Worker" => RandomDecimal(rng, 50m, 250m),
            "Young Professional" => RandomDecimal(rng, 500m, 2000m),
            "Established Professional" => RandomDecimal(rng, 1000m, 5000m),
            "Young Family" => RandomDecimal(rng, 500m, 3000m),
            "Single Parent" => RandomDecimal(rng, 200m, 800m),
            "Comfortable Retiree" => RandomDecimal(rng, 2000m, 15000m),
            "Modest Retiree" => RandomDecimal(rng, 500m, 3000m),
            _ => RandomDecimal(rng, 200m, 1000m),
        };

        txns.Add(MakeTxn(account.Id, openingDeposit, "Opening deposit",
            TransactionType.Deposit, accountStart));

        var recurringPayments = BuildRecurringPayments(rng, profile, accountStart);

        foreach (var rp in recurringPayments.Where(r => r.IsScheduled))
        {
            schedules.Add(new ScheduledPaymentSeed(
                account.Id, rp.PayeeName, rp.Amount, rp.Description, rp.Frequency, rp.FirstDate));
        }

        var runningBalance = openingDeposit;
        var nextPayDay = FindFirstPayDay(accountStart, persona.IncomeFrequency);
        var current = accountStart.AddDays(1);
        var dayEnd = now.AddDays(-1);

        // For casual workers, track when to change employer (~6-12 months)
        var employers = new List<string>(profile.Employers);
        var nextJobChange = accountStart.AddDays(rng.Next(180, 365));

        while (current <= dayEnd)
        {
            var stamp = StampAtNineAm(current, rng);

            // Casual workers occasionally change jobs
            if (employers.Count > 0 && current >= nextJobChange)
            {
                // Replace primary employer, keep second job if they have one
                string newEmployer;
                do { newEmployer = CasualEmployers[rng.Next(CasualEmployers.Length)]; }
                while (employers.Contains(newEmployer));
                employers[0] = newEmployer;
                nextJobChange = current.AddDays(rng.Next(180, 365));
            }

            // === INCOME ===
            if (current.Date >= nextPayDay.Date)
            {
                // Casual workers sometimes get no shifts at all
                var skipPay = persona.Name switch
                {
                    "Student" => rng.NextDouble() < 0.20, // 20% of weeks no work (exams, holidays)
                    "Zero-Hours Worker" => rng.NextDouble() < 0.15, // 15% of weeks no shifts
                    _ => false,
                };

                if (!skipPay)
                {
                    var income = profile.IncomeAmount;
                    // Casual workers skew low — use a right-skewed distribution
                    if (persona.Name is "Student" or "Zero-Hours Worker")
                    {
                        // Square the random to skew toward lower hours
                        var factor = (decimal)Math.Pow(rng.NextDouble(), 0.7);
                        income = Math.Round(profile.Persona.IncomeMin + (profile.Persona.IncomeMax - profile.Persona.IncomeMin) * factor, 2);
                    }
                    else
                    {
                        var variance = (decimal)(rng.NextDouble() * 2 - 1) * (decimal)profile.IncomeVariability;
                        income = Math.Round(income * (1m + variance), 2);
                    }
                    income = Math.Max(income, 50m);

                    var incomeDesc = GetIncomeDescription(rng, persona, employers);
                    txns.Add(MakeTxn(account.Id, income, incomeDesc, TransactionType.Deposit, stamp));
                    runningBalance += income;
                }

                if (persona.Name == "Young Family" && rng.NextDouble() < 0.6)
                {
                    var partnerBase = profile.IncomeAmount;
                    var partnerIncome = Math.Round(partnerBase * RandomDecimal(rng, 0.5m, 0.9m), 2);
                    txns.Add(MakeTxn(account.Id, partnerIncome, "SALARY CREDIT - PARTNER",
                        TransactionType.Deposit, stamp.AddMinutes(3)));
                    runningBalance += partnerIncome;
                }
                else if (persona.Name == "Single Parent")
                {
                    var ftb = RandomDecimal(rng, 180m, 380m);
                    txns.Add(MakeTxn(account.Id, ftb, "SERVICES AUSTRALIA - FTB",
                        TransactionType.Deposit, stamp.AddMinutes(3)));
                    runningBalance += ftb;
                }
                else if (persona.Name == "Comfortable Retiree" && rng.NextDouble() < 0.7)
                {
                    var superDrawdown = RandomDecimal(rng, 400m, 1200m);
                    txns.Add(MakeTxn(account.Id, superDrawdown,
                        rng.NextDouble() < 0.5 ? "AUSTRALIAN SUPER DRAWDOWN" : "REST SUPER DRAWDOWN",
                        TransactionType.Deposit, stamp.AddMinutes(5)));
                    runningBalance += superDrawdown;
                }

                nextPayDay = AdvanceByIncomeFrequency(nextPayDay, persona.IncomeFrequency);
            }

            // === RECURRING PAYMENTS ===
            foreach (var rp in recurringPayments)
            {
                if (IsRecurringDue(rp, current))
                {
                    if (runningBalance >= rp.Amount)
                    {
                        txns.Add(MakeTxn(account.Id, -rp.Amount, rp.Description,
                            TransactionType.DirectDebit, stamp.AddHours(rng.Next(1, 4))));
                        runningBalance -= rp.Amount;
                    }
                    else
                    {
                        // Failed - insufficient funds
                        txns.Add(MakeFailedTxn(account.Id, rp.Description,
                            "Insufficient funds", TransactionType.DirectDebit,
                            stamp.AddHours(rng.Next(1, 4))));
                    }
                    rp.AdvanceNextDue();
                }
            }

            // === DISCRETIONARY SPENDING ===
            var spendCount = GetDailySpendCount(rng, persona, current.DayOfWeek);
            for (int s = 0; s < spendCount; s++)
            {
                var (desc, amount) = PickSpend(rng, persona);
                if (amount > 0 && runningBalance >= amount)
                {
                    var spendTime = current.Date.AddHours(rng.Next(7, 22)).AddMinutes(rng.Next(0, 60));
                    if (spendTime > now) break;
                    txns.Add(MakeTxn(account.Id, -amount, desc,
                        TransactionType.Withdrawal, spendTime));
                    runningBalance -= amount;
                }
            }

            // === OCCASIONAL LARGE EXPENSES (car repair, dental, medical, vet) ===
            if (rng.NextDouble() < 0.004) // ~1.5 times per year
            {
                var (expDesc, expAmt) = PickLargeExpense(rng, persona);
                if (expAmt > 0 && runningBalance >= expAmt)
                {
                    var expTime = current.Date.AddHours(rng.Next(10, 16)).AddMinutes(rng.Next(0, 60));
                    if (expTime <= now)
                    {
                        txns.Add(MakeTxn(account.Id, -expAmt, expDesc,
                            TransactionType.Withdrawal, expTime));
                        runningBalance -= expAmt;
                    }
                }
            }

            current = current.AddDays(1);
        }

        return (txns, schedules);
    }

    private static List<RecurringPayment> BuildRecurringPayments(
        Random rng, CustomerProfile profile, DateTime accountStart)
    {
        var payments = new List<RecurringPayment>();
        var persona = profile.Persona;

        // Rent
        if (rng.NextDouble() < persona.RentLikelihood && persona.RentRange.Max > 0)
        {
            var rentAmount = RandomDecimal(rng, persona.RentRange.Min, persona.RentRange.Max);
            var isScheduled = rng.NextDouble() < 0.7;
            payments.Add(new RecurringPayment(
                "REAL ESTATE AGENTS", rentAmount,
                "RENT PAYMENT", ScheduleFrequency.Fortnightly,
                accountStart.AddDays(rng.Next(1, 14)), isScheduled));
        }

        // Mobile phone
        if (rng.NextDouble() < 0.9)
        {
            var provider = persona.Name is "Student" or "Modest Retiree"
                ? MobileProviders[2] // Vodafone
                : persona.Name is "Zero-Hours Worker" or "Single Parent"
                    ? MobileProviders[1] // Optus
                    : MobileProviders[rng.Next(MobileProviders.Length)];

            var mobileAmount = RandomDecimal(rng, provider.Min, provider.Max);
            var isScheduled = rng.NextDouble() < 0.65;
            payments.Add(new RecurringPayment(
                provider.Name, mobileAmount,
                string.Concat("DIRECT DEBIT - ", provider.Name, " MOBILE"),
                ScheduleFrequency.Monthly,
                accountStart.AddDays(rng.Next(1, 28)), isScheduled));
        }

        // Streaming services
        var maxStreaming = persona.Name switch
        {
            "Student" => 2,
            "Zero-Hours Worker" => 2,
            "Modest Retiree" => 1,
            "Comfortable Retiree" => 2,
            _ => 3,
        };
        var streamingCount = rng.Next(0, maxStreaming + 1);
        var availableStreamers = StreamingServices.OrderBy(_ => rng.Next()).Take(streamingCount).ToList();
        foreach (var streamer in availableStreamers)
        {
            var isScheduled = rng.NextDouble() < 0.5;
            payments.Add(new RecurringPayment(
                streamer.Name.ToUpper(), streamer.Amount,
                string.Concat("DIRECT DEBIT - ", streamer.Name.ToUpper()),
                ScheduleFrequency.Monthly,
                accountStart.AddDays(rng.Next(1, 28)), isScheduled));
        }

        // Utilities (quarterly - skip students and zero-hours workers)
        if (persona.Name is not "Student" and not "Zero-Hours Worker")
        {
            var elecProvider = UtilityProviders[rng.Next(2)];
            var elecAmount = RandomDecimal(rng, elecProvider.Min, elecProvider.Max);
            if (persona.Name is "Young Family" or "Established Professional")
                elecAmount *= RandomDecimal(rng, 1.1m, 1.4m);
            if (persona.Name is "Modest Retiree")
                elecAmount *= RandomDecimal(rng, 0.6m, 0.8m);
            elecAmount = Math.Round(elecAmount, 2);
            payments.Add(new RecurringPayment(
                elecProvider.Name, elecAmount,
                string.Concat("DIRECT DEBIT - ", elecProvider.Name),
                ScheduleFrequency.Quarterly,
                accountStart.AddDays(rng.Next(1, 90)), rng.NextDouble() < 0.6));

            var waterAmount = RandomDecimal(rng, UtilityProviders[2].Min, UtilityProviders[2].Max);
            if (persona.Name is "Modest Retiree")
                waterAmount *= RandomDecimal(rng, 0.5m, 0.7m);
            waterAmount = Math.Round(waterAmount, 2);
            payments.Add(new RecurringPayment(
                "SYDNEY WATER", waterAmount,
                "DIRECT DEBIT - SYDNEY WATER",
                ScheduleFrequency.Quarterly,
                accountStart.AddDays(rng.Next(1, 90)), rng.NextDouble() < 0.5));
        }

        // Car insurance
        if (rng.NextDouble() < persona.CarInsuranceLikelihood)
        {
            var insurer = CarInsuranceProviders[rng.Next(CarInsuranceProviders.Length)];
            var insuranceAmount = RandomDecimal(rng, persona.CarInsuranceRange.Min, persona.CarInsuranceRange.Max);
            var isScheduled = rng.NextDouble() < 0.75;
            payments.Add(new RecurringPayment(
                insurer, insuranceAmount,
                string.Concat("DIRECT DEBIT - ", insurer, " ", persona.CarInsuranceType.ToUpper()),
                ScheduleFrequency.Monthly,
                accountStart.AddDays(rng.Next(1, 28)), isScheduled));
        }

        return payments;
    }

    private static string GetIncomeDescription(Random rng, PersonaTemplate persona, List<string> employers) =>
        persona.Name switch
        {
            "Student" or "Zero-Hours Worker" when employers.Count > 0 =>
                string.Concat("SALARY CREDIT - ", employers[rng.Next(employers.Count)]),
            "Comfortable Retiree" or "Modest Retiree" => "SERVICES AUSTRALIA - PENSION",
            _ => "SALARY CREDIT",
        };

    private static int GetDailySpendCount(Random rng, PersonaTemplate persona, DayOfWeek dayOfWeek)
    {
        var baseCount = persona.Name switch
        {
            "Student" => rng.NextDouble() < 0.7 ? 1 : 0,
            "Zero-Hours Worker" => rng.NextDouble() < 0.8 ? rng.Next(1, 3) : 0,
            "Young Professional" => rng.NextDouble() < 0.75 ? rng.Next(1, 3) : 0,
            "Established Professional" => rng.NextDouble() < 0.7 ? rng.Next(1, 3) : 0,
            "Young Family" => rng.Next(1, 4),
            "Single Parent" => rng.NextDouble() < 0.7 ? rng.Next(1, 3) : 0,
            "Comfortable Retiree" => rng.NextDouble() < 0.5 ? rng.Next(1, 2) : 0,
            "Modest Retiree" => rng.NextDouble() < 0.4 ? 1 : 0,
            _ => rng.Next(0, 2),
        };
        if (dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            baseCount = Math.Max(baseCount, 1);
        return baseCount;
    }

    private static (string Description, decimal Amount) PickSpend(
        Random rng, PersonaTemplate persona)
    {
        var roll = rng.NextDouble();
        (string Name, decimal Min, decimal Max)[] pool;

        if (persona.Name is "Student" or "Zero-Hours Worker")
        {
            pool = roll switch
            {
                < 0.35 => DiningMerchants,
                < 0.55 => GroceryMerchants,
                < 0.70 => TransportMerchants,
                < 0.85 => RetailMerchants,
                _ => EntertainmentMerchants,
            };
        }
        else if (persona.Name is "Young Family" or "Single Parent")
        {
            pool = roll switch
            {
                < 0.40 => GroceryMerchants,
                < 0.55 => DiningMerchants,
                < 0.70 => HealthMerchants,
                < 0.82 => RetailMerchants,
                < 0.92 => TransportMerchants,
                _ => EntertainmentMerchants,
            };
        }
        else if (persona.Name is "Comfortable Retiree" or "Modest Retiree")
        {
            pool = roll switch
            {
                < 0.30 => GroceryMerchants,
                < 0.55 => HealthMerchants,
                < 0.70 => DiningMerchants,
                < 0.85 => RetailMerchants,
                _ => TransportMerchants,
            };
        }
        else
        {
            pool = roll switch
            {
                < 0.25 => GroceryMerchants,
                < 0.45 => DiningMerchants,
                < 0.60 => TransportMerchants,
                < 0.75 => RetailMerchants,
                < 0.88 => EntertainmentMerchants,
                _ => HealthMerchants,
            };
        }

        var merchant = pool[rng.Next(pool.Length)];
        var amount = RandomDecimal(rng, Math.Abs(merchant.Min), Math.Abs(merchant.Max));

        if (persona.Name is "Student" or "Modest Retiree")
            amount = Math.Round(amount * RandomDecimal(rng, 0.4m, 0.7m), 2);
        else if (persona.Name is "Zero-Hours Worker")
            amount = Math.Round(amount * RandomDecimal(rng, 0.6m, 0.85m), 2);

        return (string.Concat(merchant.Name, " ", rng.Next(100, 999).ToString("000")), amount);
    }

    private static DateTime FindFirstPayDay(DateTime accountStart, IncomeFrequency freq)
    {
        var rng = new Random(accountStart.GetHashCode());
        return freq switch
        {
            IncomeFrequency.Weekly => accountStart.AddDays(rng.Next(1, 8)),
            IncomeFrequency.Fortnightly => accountStart.AddDays(rng.Next(1, 15)),
            IncomeFrequency.Monthly => accountStart.AddDays(rng.Next(1, 29)),
            _ => accountStart.AddDays(7),
        };
    }

    private static DateTime AdvanceByIncomeFrequency(DateTime current, IncomeFrequency frequency) =>
        frequency switch
        {
            IncomeFrequency.Weekly => current.AddDays(7),
            IncomeFrequency.Fortnightly => current.AddDays(14),
            IncomeFrequency.Monthly => current.AddMonths(1),
            _ => current.AddDays(14),
        };

    private static bool IsRecurringDue(RecurringPayment recurring, DateTime date) =>
        date.Date >= recurring.NextDue.Date;

    private static void GenerateScheduledPayments(
        BankDbContext db, List<ScheduledPaymentSeed> seeds, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);
        foreach (var s in seeds)
        {
            var startDate = DateOnly.FromDateTime(s.FirstBillingDate);
            var nextDue = startDate;
            while (nextDue < today)
            {
                nextDue = AdvanceNextDue(nextDue, s.Frequency);
            }

            db.ScheduledPayments.Add(new ScheduledPayment
            {
                AccountId = s.AccountId,
                PayeeName = s.PayeeName,
                Amount = s.Amount,
                Description = s.Description,
                Frequency = s.Frequency,
                StartDate = startDate,
                NextDueDate = nextDue,
                IsActive = true,
            });
        }
        db.SaveChanges();
    }

    private static DateOnly AdvanceNextDue(DateOnly current, ScheduleFrequency frequency) =>
        frequency switch
        {
            ScheduleFrequency.Weekly => current.AddDays(7),
            ScheduleFrequency.Fortnightly => current.AddDays(14),
            ScheduleFrequency.Monthly => current.AddMonths(1),
            ScheduleFrequency.Quarterly => current.AddMonths(3),
            ScheduleFrequency.Yearly => current.AddYears(1),
            _ => current.AddMonths(1),
        };

    private static void FlushTransactions(BankDbContext db, List<Transaction> buffer)
    {
        if (buffer.Count == 0) return;
        db.Transactions.AddRange(buffer);
        db.SaveChanges();
        buffer.Clear();
    }

    private static void UpdateAccountBalances(BankDbContext db)
    {
        var balances = db.Transactions
            .Where(t => t.Status == TransactionStatus.Settled)
            .GroupBy(t => t.AccountId)
            .Select(g => new { AccountId = g.Key, Total = g.Sum(t => t.Amount) })
            .ToList();

        foreach (var b in balances)
        {
            var account = db.Accounts.Find(b.AccountId);
            if (account != null)
            {
                account.Balance = b.Total;
                db.Entry(account).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }
        }
        db.SaveChanges();
    }

    private static void MarkRecentWithdrawalsAsPending(BankDbContext db, DateTime now)
    {
        var cutoff = now.AddDays(-2);
        var recentWithdrawals = db.Transactions
            .Where(t => t.TransactionType == TransactionType.Withdrawal
                && t.Status == TransactionStatus.Settled
                && t.CreatedAt > cutoff
                && t.Amount < 0)
            .ToList();

        foreach (var txn in recentWithdrawals)
        {
            txn.Status = TransactionStatus.Pending;
            txn.SettledAt = null;
            db.Entry(txn).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            var account = db.Accounts.Find(txn.AccountId)!;
            account.Balance -= txn.Amount;
            db.Entry(account).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
        }
        db.SaveChanges();
    }

    private static void GenerateBalanceSnapshots(BankDbContext db)
    {
        var accounts = db.Accounts.Where(a => a.IsActive).ToList();

        foreach (var account in accounts)
        {
            var lastTxnDate = db.Transactions
                .Where(t => t.AccountId == account.Id)
                .Max(t => (DateTime?)t.CreatedAt);

            if (lastTxnDate == null) continue;

            var snapshotDate = DateOnly.FromDateTime(lastTxnDate.Value);
            var pendingHolds = db.Transactions
                .Where(t => t.AccountId == account.Id
                    && t.Status == TransactionStatus.Pending
                    && t.Amount < 0)
                .Sum(t => t.Amount);

            db.AccountBalanceSnapshots.Add(new AccountBalanceSnapshot
            {
                AccountId = account.Id,
                SnapshotDate = snapshotDate,
                LedgerBalance = account.Balance,
                AvailableBalance = account.Balance + pendingHolds,
            });
        }
        db.SaveChanges();
    }

    private static void SetLastProcessedDate(BankDbContext db, DateTime now)
    {
        var setting = db.SystemSettings.FirstOrDefault(s => s.Key == "LastProcessedDate");
        var value = DateOnly.FromDateTime(now.AddDays(-1)).ToString("yyyy-MM-dd");
        if (setting is null)
        {
            db.SystemSettings.Add(new SystemSettings { Key = "LastProcessedDate", Value = value });
        }
        else
        {
            setting.Value = value;
        }
        db.SaveChanges();
    }

    private static Transaction MakeTxn(
        int accountId, decimal amount, string description,
        TransactionType type, DateTime createdAt) =>
        new()
        {
            AccountId = accountId,
            Amount = amount,
            Description = description,
            TransactionType = type,
            Status = TransactionStatus.Settled,
            SettledAt = createdAt,
            CreatedAt = createdAt,
        };

    private static Transaction MakeFailedTxn(
        int accountId, string description, string failureReason,
        TransactionType type, DateTime createdAt) =>
        new()
        {
            AccountId = accountId,
            Amount = 0m,
            Description = description,
            TransactionType = type,
            Status = TransactionStatus.Failed,
            FailureReason = failureReason,
            CreatedAt = createdAt,
        };

    private static DateTime StampAtNineAm(DateTime date, Random rng) =>
        date.Date.AddHours(9).AddMinutes(rng.Next(0, 30));

    private static (string Description, decimal Amount) PickLargeExpense(
        Random rng, PersonaTemplate persona)
    {
        var expenses = new (string Desc, decimal Min, decimal Max)[]
        {
            ("ULTRATUNE AUTO REPAIR", 250m, 1200m),
            ("MYCAR SERVICE", 200m, 900m),
            ("DR SMITH DENTAL", 150m, 600m),
            ("SPECSAVERS", 100m, 350m),
            ("PRICELINE PHARMACY", 80m, 250m),
        };
        var expense = expenses[rng.Next(expenses.Length)];
        var amount = RandomDecimal(rng, expense.Min, expense.Max);
        if (persona.Name is "Student" or "Zero-Hours Worker" or "Modest Retiree")
            amount = Math.Round(amount * RandomDecimal(rng, 0.4m, 0.65m), 2);
        return ($"{expense.Desc} {rng.Next(100, 999):000}", amount);
    }

    private static decimal RandomDecimal(Random rng, decimal min, decimal max) =>
        Math.Round(min + (max - min) * (decimal)rng.NextDouble(), 2);

    private enum IncomeFrequency
    {
        Weekly,
        Fortnightly,
        Monthly
    }

    private sealed record PersonaTemplate(
        string Name,
        int Weight,
        int MinAge,
        int MaxAge,
        decimal IncomeMin,
        decimal IncomeMax,
        IncomeFrequency IncomeFrequency,
        (decimal Min, decimal Max) RentRange,
        double RentLikelihood,
        double CarInsuranceLikelihood,
        (decimal Min, decimal Max) CarInsuranceRange,
        string CarInsuranceType);

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
        double IncomeVariability,
        List<string> Employers);

    private sealed record ScheduledPaymentSeed(
        int AccountId,
        string PayeeName,
        decimal Amount,
        string Description,
        ScheduleFrequency Frequency,
        DateTime FirstBillingDate);

    private class RecurringPayment(
        string payeeName,
        decimal amount,
        string description,
        ScheduleFrequency frequency,
        DateTime firstDate,
        bool isScheduled)
    {
        public string PayeeName { get; } = payeeName;
        public decimal Amount { get; } = amount;
        public string Description { get; } = description;
        public ScheduleFrequency Frequency { get; } = frequency;
        public bool IsScheduled { get; } = isScheduled;
        public DateTime FirstDate { get; } = firstDate;
        public DateTime NextDue { get; private set; } = firstDate;

        public void AdvanceNextDue()
        {
            NextDue = Frequency switch
            {
                ScheduleFrequency.Weekly => NextDue.AddDays(7),
                ScheduleFrequency.Fortnightly => NextDue.AddDays(14),
                ScheduleFrequency.Monthly => NextDue.AddMonths(1),
                ScheduleFrequency.Quarterly => NextDue.AddMonths(3),
                ScheduleFrequency.Yearly => NextDue.AddYears(1),
                _ => NextDue.AddMonths(1),
            };
        }
    }
}
