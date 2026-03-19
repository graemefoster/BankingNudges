using BankOfGraeme.Api.Models;
using BankOfGraeme.Api.Services;
using BankOfGraeme.Api.Services.InterestCalculation;
using Microsoft.EntityFrameworkCore;

namespace BankOfGraeme.Api.Data;

public static class SeedData
{
    private const int CustomerCount = 150;
    private const int RandomSeed = 42;
    private const string Bsb = "062-000";
    private const int TransactionBatchSize = 5_000;
    private const decimal TransactionInterestRate = 3.00m;
    private const decimal SavingsInterestRate = 5.00m;
    private const decimal SavingsBonusInterestRate = 4.50m;

    // Savings account seed configuration per persona
    private static readonly Dictionary<string, SavingsSeedConfig> SavingsConfigs = new()
    {
        ["Student"] = new(0.25, 200m, 1000m, 50m, 150m, 0.60, 0.40, 50m, 500m),
        ["Zero-Hours Worker"] = new(0.10, 100m, 500m, 30m, 100m, 0.40, 0.50, 50m, 300m),
        ["Young Professional"] = new(0.55, 2000m, 8000m, 400m, 1000m, 0.90, 0.15, 500m, 3000m),
        ["Established Professional"] = new(0.60, 10000m, 40000m, 800m, 2500m, 0.95, 0.10, 1000m, 8000m),
        ["Young Family"] = new(0.40, 3000m, 12000m, 200m, 600m, 0.85, 0.30, 500m, 3000m),
        ["Single Parent"] = new(0.15, 500m, 2000m, 50m, 200m, 0.50, 0.55, 200m, 1000m),
        ["Comfortable Retiree"] = new(0.80, 30000m, 120000m, 200m, 800m, 0.85, 0.08, 1000m, 5000m),
        ["Modest Retiree"] = new(0.50, 5000m, 20000m, 50m, 200m, 0.75, 0.25, 500m, 2000m),
    };

    // Home loan seed configuration per persona
    private static readonly Dictionary<string, HomeLoanSeedConfig> HomeLoanConfigs = new()
    {
        ["Young Professional"] = new(400_000m, 600_000m, 5.99m, 6.49m, 360, 0.25, 5_000m, 20_000m),
        ["Established Professional"] = new(650_000m, 900_000m, 5.49m, 6.29m, 300, 0.60, 20_000m, 80_000m),
        ["Young Family"] = new(550_000m, 800_000m, 5.79m, 6.49m, 360, 0.50, 10_000m, 40_000m),
    };

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

    // === HOLIDAY / TRAVEL DATA ===
    private const decimal InternationalFeeRate = 0.03m; // 3% international transaction fee

    private static readonly string[] TravelAgents =
        ["FLIGHT CENTRE", "WEBJET", "SKYSCANNER", "BOOKING.COM", "EXPEDIA", "QANTAS", "VIRGIN AUSTRALIA"];

    private sealed record HolidayDestination(
        string Name, string Currency, decimal ExchangeRate,
        (string Name, decimal Min, decimal Max)[] Merchants);

    private static readonly HolidayDestination[] Destinations =
    [
        new("Bali", "IDR", 10_300m, [
            ("WARUNG MADE BALI", 45_000m, 250_000m), ("BALI BEACH CLUB", 150_000m, 500_000m),
            ("SEMINYAK SQUARE", 80_000m, 600_000m), ("GRAB BALI", 25_000m, 120_000m),
            ("WATERBOM BALI", 200_000m, 400_000m), ("CIRCLE K BALI", 15_000m, 80_000m),
            ("BALI SPA & MASSAGE", 100_000m, 350_000m), ("CARREFOUR BALI", 50_000m, 300_000m),
        ]),
        new("Thailand", "THB", 23.5m, [
            ("7-ELEVEN BANGKOK", 30m, 200m), ("CHATUCHAK MARKET", 100m, 800m),
            ("GRAB THAILAND", 50m, 250m), ("SIAM PARAGON", 200m, 2000m),
            ("STREET FOOD BKK", 40m, 150m), ("FULL MOON PARTY", 300m, 1500m),
            ("TEMPLE TOUR TH", 100m, 500m), ("CENTRAL PATTANA", 150m, 1200m),
        ]),
        new("Fiji", "FJD", 1.45m, [
            ("NADI MARKET", 8m, 60m), ("FIJI BEACH RESORT", 25m, 120m),
            ("PORTS OF FIJI", 15m, 80m), ("ISLAND HOPPER FIJI", 40m, 150m),
            ("ROSIE HOLIDAYS FIJI", 20m, 90m), ("FIJI WATER SPORTS", 30m, 100m),
        ]),
        new("Japan", "JPY", 97.0m, [
            ("LAWSON JAPAN", 200m, 1500m), ("UNIQLO TOKYO", 1000m, 8000m),
            ("SUICA TOP UP", 1000m, 5000m), ("RAMEN ICHIRAN", 800m, 2000m),
            ("DON QUIJOTE", 500m, 5000m), ("TEAMLAB TOKYO", 2000m, 4000m),
            ("7-ELEVEN JAPAN", 150m, 800m), ("SHINKANSEN JR", 5000m, 15000m),
        ]),
        new("New Zealand", "NZD", 1.08m, [
            ("PAK N SAVE NZ", 15m, 120m), ("BUNGY NZ", 150m, 300m),
            ("KIWI EXPERIENCE", 40m, 200m), ("COUNTDOWN NZ", 12m, 90m),
            ("FERGBURGER NZ", 15m, 35m), ("INTERISLANDER NZ", 50m, 180m),
        ]),
        new("Europe", "EUR", 0.61m, [
            ("CARREFOUR EU", 8m, 80m), ("EIFFEL TOWER PARIS", 15m, 30m),
            ("EURAIL PASS", 50m, 250m), ("COLOSSEUM ROMA", 12m, 25m),
            ("TAPAS BAR BARCELONA", 10m, 45m), ("ZARA EU", 20m, 120m),
            ("MUSEUM EU", 10m, 25m), ("HOTEL EU", 60m, 200m),
        ]),
        new("USA", "USD", 0.65m, [
            ("WALMART US", 10m, 100m), ("UBER USA", 8m, 45m),
            ("UNIVERSAL STUDIOS", 80m, 200m), ("IN-N-OUT BURGER", 8m, 20m),
            ("TARGET USA", 15m, 120m), ("NYC YELLOW CAB", 10m, 40m),
            ("TIMES SQUARE NYC", 20m, 80m), ("WHOLE FOODS US", 15m, 90m),
        ]),
        new("UK", "GBP", 0.52m, [
            ("TESCO UK", 8m, 60m), ("LONDON EYE", 20m, 35m),
            ("TUBE OYSTER UK", 5m, 20m), ("PRET A MANGER UK", 5m, 15m),
            ("PRIMARK UK", 10m, 60m), ("THEATRE WEST END", 30m, 100m),
            ("BRITISH MUSEUM", 0m, 15m), ("WETHERSPOONS UK", 8m, 30m),
        ]),
    ];

    // Budget destinations for price-sensitive personas
    private static readonly int[] BudgetDestinationIndices = [0, 1]; // Bali, Thailand
    private static readonly int[] FamilyDestinationIndices = [0, 2, 3, 4]; // Bali, Fiji, Japan, NZ

    private sealed record HolidayConfig(
        double Probability, int MinDays, int MaxDays,
        int[] EligibleDestinations, decimal FlightMin, decimal FlightMax);

    private static readonly Dictionary<string, HolidayConfig> HolidayConfigs = new()
    {
        ["Student"] = new(0.15, 7, 14, BudgetDestinationIndices, 400m, 800m),
        ["Zero-Hours Worker"] = new(0.05, 5, 7, BudgetDestinationIndices, 350m, 600m),
        ["Young Professional"] = new(0.35, 7, 14, [0, 1, 2, 3, 4, 5, 6, 7], 600m, 2500m),
        ["Established Professional"] = new(0.45, 7, 14, [2, 3, 4, 5, 6, 7], 1200m, 4000m),
        ["Young Family"] = new(0.25, 7, 14, FamilyDestinationIndices, 2000m, 6000m),
        ["Single Parent"] = new(0.10, 5, 7, BudgetDestinationIndices, 1000m, 2500m),
        ["Comfortable Retiree"] = new(0.40, 10, 21, [0, 1, 2, 3, 4, 5, 6, 7], 1500m, 5000m),
        ["Modest Retiree"] = new(0.12, 7, 10, [0, 1, 2], 600m, 1500m),
    };

    // === BRANCH & ATM SEED DATA ===
    // Real Australian locations for geolocation experiments.
    // Branches are Bank of Graeme physical branches; ATMs include branch-attached and standalone.

    private sealed record BranchSeed(string Name, string Address, string Suburb, string State, string Postcode, decimal Lat, decimal Lng);
    private sealed record AtmSeed(string LocationName, string Address, string Suburb, string State, string Postcode, decimal Lat, decimal Lng, int? BranchIndex);

    private static readonly BranchSeed[] BranchSeeds =
    [
        new("Bank of Graeme Sydney CBD", "123 George St", "Sydney", "NSW", "2000", -33.8688m, 151.2093m),
        new("Bank of Graeme Parramatta", "45 Church St", "Parramatta", "NSW", "2150", -33.8148m, 151.0034m),
        new("Bank of Graeme Chatswood", "12 Victoria Ave", "Chatswood", "NSW", "2067", -33.7969m, 151.1816m),
        new("Bank of Graeme Bondi Junction", "8 Oxford St", "Bondi Junction", "NSW", "2022", -33.8919m, 151.2501m),
        new("Bank of Graeme Melbourne CBD", "200 Collins St", "Melbourne", "VIC", "3000", -37.8136m, 144.9631m),
        new("Bank of Graeme South Melbourne", "55 Clarendon St", "South Melbourne", "VIC", "3205", -37.8310m, 144.9567m),
        new("Bank of Graeme Box Hill", "15 Main St", "Box Hill", "VIC", "3128", -37.8186m, 145.1215m),
        new("Bank of Graeme Brisbane CBD", "100 Queen St", "Brisbane", "QLD", "4000", -27.4698m, 153.0251m),
        new("Bank of Graeme Gold Coast", "30 Cavill Ave", "Surfers Paradise", "QLD", "4217", -28.0167m, 153.4000m),
        new("Bank of Graeme Perth CBD", "80 St Georges Tce", "Perth", "WA", "6000", -31.9505m, 115.8605m),
        new("Bank of Graeme Adelaide CBD", "50 King William St", "Adelaide", "SA", "5000", -34.9285m, 138.6007m),
        new("Bank of Graeme Hobart", "22 Elizabeth St", "Hobart", "TAS", "7000", -42.8821m, 147.3272m),
        new("Bank of Graeme Canberra", "10 London Circuit", "Canberra", "ACT", "2601", -35.2809m, 149.1300m),
        new("Bank of Graeme Newcastle", "75 Hunter St", "Newcastle", "NSW", "2300", -32.9283m, 151.7817m),
        new("Bank of Graeme Wollongong", "18 Crown St", "Wollongong", "NSW", "2500", -34.4278m, 150.8931m),
        new("Bank of Graeme Geelong", "40 Moorabool St", "Geelong", "VIC", "3220", -38.1499m, 144.3503m),
        new("Bank of Graeme Townsville", "5 Flinders St", "Townsville", "QLD", "4810", -19.2590m, 146.7867m),
        new("Bank of Graeme Bendigo", "25 Mitchell St", "Bendigo", "VIC", "3550", -36.7570m, 144.2794m),
    ];

    private static readonly AtmSeed[] AtmSeeds =
    [
        // Branch ATMs (one per branch)
        new("Bank of Graeme ATM - Sydney CBD", "123 George St", "Sydney", "NSW", "2000", -33.8688m, 151.2093m, 0),
        new("Bank of Graeme ATM - Parramatta", "45 Church St", "Parramatta", "NSW", "2150", -33.8148m, 151.0034m, 1),
        new("Bank of Graeme ATM - Chatswood", "12 Victoria Ave", "Chatswood", "NSW", "2067", -33.7969m, 151.1816m, 2),
        new("Bank of Graeme ATM - Bondi Junction", "8 Oxford St", "Bondi Junction", "NSW", "2022", -33.8919m, 151.2501m, 3),
        new("Bank of Graeme ATM - Melbourne CBD", "200 Collins St", "Melbourne", "VIC", "3000", -37.8136m, 144.9631m, 4),
        new("Bank of Graeme ATM - South Melbourne", "55 Clarendon St", "South Melbourne", "VIC", "3205", -37.8310m, 144.9567m, 5),
        new("Bank of Graeme ATM - Box Hill", "15 Main St", "Box Hill", "VIC", "3128", -37.8186m, 145.1215m, 6),
        new("Bank of Graeme ATM - Brisbane CBD", "100 Queen St", "Brisbane", "QLD", "4000", -27.4698m, 153.0251m, 7),
        new("Bank of Graeme ATM - Gold Coast", "30 Cavill Ave", "Surfers Paradise", "QLD", "4217", -28.0167m, 153.4000m, 8),
        new("Bank of Graeme ATM - Perth CBD", "80 St Georges Tce", "Perth", "WA", "6000", -31.9505m, 115.8605m, 9),
        new("Bank of Graeme ATM - Adelaide CBD", "50 King William St", "Adelaide", "SA", "5000", -34.9285m, 138.6007m, 10),
        new("Bank of Graeme ATM - Hobart", "22 Elizabeth St", "Hobart", "TAS", "7000", -42.8821m, 147.3272m, 11),
        new("Bank of Graeme ATM - Canberra", "10 London Circuit", "Canberra", "ACT", "2601", -35.2809m, 149.1300m, 12),
        new("Bank of Graeme ATM - Newcastle", "75 Hunter St", "Newcastle", "NSW", "2300", -32.9283m, 151.7817m, 13),
        new("Bank of Graeme ATM - Wollongong", "18 Crown St", "Wollongong", "NSW", "2500", -34.4278m, 150.8931m, 14),
        new("Bank of Graeme ATM - Geelong", "40 Moorabool St", "Geelong", "VIC", "3220", -38.1499m, 144.3503m, 15),
        new("Bank of Graeme ATM - Townsville", "5 Flinders St", "Townsville", "QLD", "4810", -19.2590m, 146.7867m, 16),
        new("Bank of Graeme ATM - Bendigo", "25 Mitchell St", "Bendigo", "VIC", "3550", -36.7570m, 144.2794m, 17),
        // Standalone ATMs (shopping centres, unis, transport hubs)
        new("Westfield Sydney ATM", "188 Pitt St", "Sydney", "NSW", "2000", -33.8718m, 151.2086m, null),
        new("UNSW Kensington ATM", "High St", "Kensington", "NSW", "2033", -33.9173m, 151.2313m, null),
        new("Central Station ATM", "Eddy Ave", "Haymarket", "NSW", "2000", -33.8833m, 151.2060m, null),
        new("Westfield Parramatta ATM", "159 Church St", "Parramatta", "NSW", "2150", -33.8168m, 151.0054m, null),
        new("Macquarie Centre ATM", "Herring Rd", "Macquarie Park", "NSW", "2113", -33.7749m, 151.1263m, null),
        new("Chadstone Shopping Centre ATM", "1341 Dandenong Rd", "Chadstone", "VIC", "3148", -37.8854m, 145.0831m, null),
        new("Melbourne Central ATM", "211 La Trobe St", "Melbourne", "VIC", "3000", -37.8103m, 144.9629m, null),
        new("Flinders Street Station ATM", "Flinders St", "Melbourne", "VIC", "3000", -37.8183m, 144.9671m, null),
        new("University of Melbourne ATM", "Grattan St", "Parkville", "VIC", "3052", -37.7963m, 144.9614m, null),
        new("Carindale Shopping Centre ATM", "1151 Creek Rd", "Carindale", "QLD", "4152", -27.5049m, 153.1021m, null),
        new("Pacific Fair ATM", "Hooker Blvd", "Broadbeach", "QLD", "4218", -28.0368m, 153.4312m, null),
        new("Garden City Perth ATM", "125 Riseley St", "Booragoon", "WA", "6154", -32.0395m, 115.8400m, null),
        new("Rundle Mall ATM", "Rundle Mall", "Adelaide", "SA", "5000", -34.9225m, 138.6043m, null),
        new("ANU Campus ATM", "University Ave", "Acton", "ACT", "2601", -35.2777m, 149.1185m, null),
        new("Charlestown Square ATM", "30 Pearson St", "Charlestown", "NSW", "2290", -32.9600m, 151.6933m, null),
        new("Wollongong Central ATM", "200 Crown St", "Wollongong", "NSW", "2500", -34.4250m, 150.8935m, null),
    ];

    // ATM withdrawal config per persona: (avg visits per month, min amount, max amount)
    private sealed record AtmUsageConfig(double MonthlyFrequency, decimal AmountMin, decimal AmountMax);

    private static readonly Dictionary<string, AtmUsageConfig> AtmConfigs = new()
    {
        ["Student"] = new(2.5, 20m, 80m),
        ["Zero-Hours Worker"] = new(3.5, 40m, 100m),
        ["Young Professional"] = new(1.5, 50m, 100m),
        ["Established Professional"] = new(1.5, 50m, 200m),
        ["Young Family"] = new(2.5, 40m, 100m),
        ["Single Parent"] = new(2.5, 30m, 80m),
        ["Comfortable Retiree"] = new(4.0, 50m, 200m),
        ["Modest Retiree"] = new(3.5, 20m, 100m),
    };

    // Branch deposit config per persona: (avg deposits per month, min amount, max amount)
    private sealed record BranchDepositConfig(double MonthlyFrequency, decimal AmountMin, decimal AmountMax);

    private static readonly Dictionary<string, BranchDepositConfig> BranchDepositConfigs = new()
    {
        ["Student"] = new(0.5, 50m, 300m),
        ["Zero-Hours Worker"] = new(2.5, 100m, 500m),
        ["Young Professional"] = new(0.1, 50m, 200m),
        ["Established Professional"] = new(0.1, 50m, 200m),
        ["Young Family"] = new(0.5, 50m, 500m),
        ["Single Parent"] = new(1.5, 50m, 300m),
        ["Comfortable Retiree"] = new(2.5, 100m, 500m),
        ["Modest Retiree"] = new(2.5, 50m, 300m),
    };

    private sealed record HolidayPeriod(
        DateTime DepartureDate, int DurationDays, HolidayDestination Destination,
        decimal FlightCost, DateTime BookingDate);

    /// <summary>
    /// Guaranteed holiday scenarios for spotlight customers, anchored to virtual "now".
    /// These ensure specific customers are always mid-trip, just-booked, or just-returned
    /// regardless of random seed — so demo screens always have interesting travel data.
    /// </summary>
    private static List<HolidayPeriod> GetGuaranteedHolidays(string firstName, string lastName, DateTime now)
    {
        var holidays = new List<HolidayPeriod>();

        // Chloe Martin — currently on holiday in Japan (departed 4 days ago, 10-day trip)
        if (firstName == "Chloe" && lastName == "Martin")
        {
            var departure = now.AddDays(-4);
            var dest = Destinations[3]; // Japan
            holidays.Add(new HolidayPeriod(departure, 10, dest, 1850m,
                departure.AddDays(-21)));
        }
        // Ethan Ross — just booked a Europe trip via Qantas (booking 3 days ago, departs in 18 days)
        else if (firstName == "Ethan" && lastName == "Ross")
        {
            var departure = now.AddDays(18);
            var dest = Destinations[5]; // Europe
            holidays.Add(new HolidayPeriod(departure, 12, dest, 3200m,
                now.AddDays(-3)));
        }
        // Grace Turner — just returned from a family Fiji trip (ended 2 days ago)
        else if (firstName == "Grace" && lastName == "Turner")
        {
            var returnDate = now.AddDays(-2);
            var duration = 9;
            var departure = returnDate.AddDays(-duration);
            var dest = Destinations[2]; // Fiji
            holidays.Add(new HolidayPeriod(departure, duration, dest, 4200m,
                departure.AddDays(-25)));
        }
        // Gabriel White — booked a Bali trip via Flight Centre last week, departs in 10 days
        else if (firstName == "Gabriel" && lastName == "White")
        {
            var departure = now.AddDays(10);
            var dest = Destinations[0]; // Bali
            holidays.Add(new HolidayPeriod(departure, 14, dest, 2800m,
                now.AddDays(-7)));
        }

        return holidays;
    }

    private static readonly Dictionary<string, string[]> SavingsWithdrawalReasons = new()
    {
        ["Student"] = ["TEXTBOOKS", "LAPTOP", "CONCERT TICKETS", "UNI FEES"],
        ["Zero-Hours Worker"] = ["EMERGENCY", "BOND PAYMENT", "CAR REPAIR"],
        ["Young Professional"] = ["HOLIDAY", "FURNITURE", "COURSE FEES"],
        ["Established Professional"] = ["HOME RENOVATION", "HOLIDAY", "INVESTMENT"],
        ["Young Family"] = ["SCHOOL FEES", "FAMILY HOLIDAY", "APPLIANCES", "CAR SERVICE"],
        ["Single Parent"] = ["CAR REPAIR", "SCHOOL FEES", "MEDICAL"],
        ["Comfortable Retiree"] = ["HOLIDAY", "HOME MAINTENANCE", "GIFT"],
        ["Modest Retiree"] = ["DENTAL", "HOME REPAIR", "MEDICAL", "SPECTACLES"],
    };

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
        // Seed branches and ATMs first — they're referenced by transactions
        var branches = SeedBranches(db);
        var atms = SeedAtms(db, branches);

        var transactionBuffer = new List<Transaction>(TransactionBatchSize);
        var scheduledPayments = new List<ScheduledPaymentSeed>();
        int accountNumber = 10000001;
        int spotlightIndex = 0;

        for (int i = 0; i < CustomerCount; i++)
        {
            var profile = CreateCustomerProfile(rng, i, now, ref spotlightIndex);
            db.Customers.Add(profile.Customer);
            db.SaveChanges();

            // Determine home loan eligibility before generating transaction history
            // (so we can suppress rent for homeowners)
            var hasHomeLoan = HomeLoanConfigs.TryGetValue(profile.Persona.Name, out var loanConfig)
                && rng.NextDouble() < loanConfig.Likelihood;

            // Most home loan customers use the offset as their everyday account
            // (~80%). The remainder keep a separate transaction account.
            var offsetIsEveryday = hasHomeLoan && rng.NextDouble() < 0.8;

            Account everydayAccount;
            Account? homeLoanAccount = null;
            Account? offsetAccount = null;
            decimal loanAmount = 0;
            decimal interestRate = 0;

            if (offsetIsEveryday)
            {
                // Create home loan + offset first; offset IS the everyday account
                loanAmount = Math.Round(RandomDecimal(rng, loanConfig!.LoanMin, loanConfig.LoanMax) / 1000m) * 1000m;
                interestRate = RandomDecimal(rng, loanConfig.RateMin, loanConfig.RateMax);

                homeLoanAccount = new Account
                {
                    CustomerId = profile.Customer.Id,
                    AccountType = AccountType.HomeLoan,
                    Bsb = Bsb,
                    AccountNumber = (accountNumber++).ToString(),
                    Name = "Home Loan",
                    Balance = 0m,
                    IsActive = true,
                    LoanAmount = loanAmount,
                    InterestRate = interestRate,
                    LoanTermMonths = loanConfig.TermMonths,
                };
                db.Accounts.Add(homeLoanAccount);
                db.SaveChanges();

                offsetAccount = new Account
                {
                    CustomerId = profile.Customer.Id,
                    AccountType = AccountType.Offset,
                    Bsb = Bsb,
                    AccountNumber = (accountNumber++).ToString(),
                    Name = "Offset Account",
                    Balance = 0m,
                    IsActive = true,
                    HomeLoanAccountId = homeLoanAccount.Id,
                };
                db.Accounts.Add(offsetAccount);
                db.SaveChanges();

                everydayAccount = offsetAccount;
            }
            else
            {
                // Standard transaction account
                everydayAccount = new Account
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
                db.Accounts.Add(everydayAccount);
                db.SaveChanges();
            }

            var (txns, schedules) = GenerateTransactionHistory(rng, everydayAccount, profile, now, hasHomeLoan, branches, atms);

            foreach (var txn in txns)
            {
                transactionBuffer.Add(txn);
                if (transactionBuffer.Count >= TransactionBatchSize)
                    FlushTransactions(db, transactionBuffer);
            }
            scheduledPayments.AddRange(schedules);

            // Home loan history
            if (hasHomeLoan)
            {
                if (!offsetIsEveryday)
                {
                    // Create home loan + offset for customers who keep a separate transaction account
                    loanAmount = Math.Round(RandomDecimal(rng, loanConfig!.LoanMin, loanConfig.LoanMax) / 1000m) * 1000m;
                    interestRate = RandomDecimal(rng, loanConfig.RateMin, loanConfig.RateMax);

                    homeLoanAccount = new Account
                    {
                        CustomerId = profile.Customer.Id,
                        AccountType = AccountType.HomeLoan,
                        Bsb = Bsb,
                        AccountNumber = (accountNumber++).ToString(),
                        Name = "Home Loan",
                        Balance = 0m,
                        IsActive = true,
                        LoanAmount = loanAmount,
                        InterestRate = interestRate,
                        LoanTermMonths = loanConfig!.TermMonths,
                    };
                    db.Accounts.Add(homeLoanAccount);
                    db.SaveChanges();

                    offsetAccount = new Account
                    {
                        CustomerId = profile.Customer.Id,
                        AccountType = AccountType.Offset,
                        Bsb = Bsb,
                        AccountNumber = (accountNumber++).ToString(),
                        Name = "Offset Account",
                        Balance = 0m,
                        IsActive = true,
                        HomeLoanAccountId = homeLoanAccount.Id,
                    };
                    db.Accounts.Add(offsetAccount);
                    db.SaveChanges();
                }

                // Precompute monthly offset balance snapshots when offset is the everyday account
                Dictionary<DateOnly, decimal>? offsetMonthlyBalances = null;
                if (offsetIsEveryday)
                {
                    offsetMonthlyBalances = new Dictionary<DateOnly, decimal>();
                    var running = 0m;
                    foreach (var txn in txns.OrderBy(t => t.CreatedAt))
                    {
                        running += txn.Amount;
                        var month = new DateOnly(txn.CreatedAt.Year, txn.CreatedAt.Month, 1);
                        offsetMonthlyBalances[month] = running;
                    }
                }

                var (loanTxns, loanSchedule) = GenerateHomeLoanHistory(
                    rng, homeLoanAccount!, offsetAccount!, everydayAccount,
                    loanConfig!, loanAmount, interestRate, profile, now,
                    offsetIsEveryday, offsetMonthlyBalances);

                foreach (var txn in loanTxns)
                {
                    transactionBuffer.Add(txn);
                    if (transactionBuffer.Count >= TransactionBatchSize)
                        FlushTransactions(db, transactionBuffer);
                }
                scheduledPayments.Add(loanSchedule);
            }

            // === SAVINGS ACCOUNT ===
            var hasSavings = SavingsConfigs.TryGetValue(profile.Persona.Name, out var savingsConfig)
                && rng.NextDouble() < savingsConfig.Likelihood;

            if (hasSavings)
            {
                var savingsAccount = new Account
                {
                    CustomerId = profile.Customer.Id,
                    AccountType = AccountType.Savings,
                    Bsb = Bsb,
                    AccountNumber = (accountNumber++).ToString(),
                    Name = "Savings",
                    Balance = 0m,
                    IsActive = true,
                    InterestRate = SavingsInterestRate,
                    BonusInterestRate = SavingsBonusInterestRate,
                };
                db.Accounts.Add(savingsAccount);
                db.SaveChanges();

                var savingsTxns = GenerateSavingsHistory(
                    rng, savingsAccount, everydayAccount, savingsConfig!, profile, now);
                foreach (var txn in savingsTxns)
                {
                    transactionBuffer.Add(txn);
                    if (transactionBuffer.Count >= TransactionBatchSize)
                        FlushTransactions(db, transactionBuffer);
                }
            }
        }

        FlushTransactions(db, transactionBuffer);
        UpdateAccountBalances(db);
        MarkRecentWithdrawalsAsPending(db, now);
        GenerateScheduledPayments(db, scheduledPayments, now);
        SeedFraudScenario(db, now, ref accountNumber);
        GenerateBalanceSnapshots(db);
        SeedCustomerHolidays(db, now);
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
        GenerateTransactionHistory(Random rng, Account account, CustomerProfile profile, DateTime now, bool hasHomeLoan,
            List<Branch> branches, List<Atm> atms)
    {
        var txns = new List<Transaction>();
        var schedules = new List<ScheduledPaymentSeed>();
        var accountStart = profile.Customer.CreatedAt;
        var persona = profile.Persona;

        // Assign a home branch for this customer (creates geographic clustering)
        var homeBranchIndex = profile.Customer.Id % branches.Count;
        var homeBranch = branches[homeBranchIndex];
        // Home ATMs: ATMs attached to the home branch, or nearby standalone ones
        var homeAtms = atms.Where(a => a.BranchId == homeBranch.Id).ToList();
        if (homeAtms.Count == 0) homeAtms = [atms[homeBranchIndex % atms.Count]];

        // ATM/branch usage configs for this persona
        var atmConfig = AtmConfigs.GetValueOrDefault(persona.Name);
        var branchDepositConfig = BranchDepositConfigs.GetValueOrDefault(persona.Name);
        var atmDailyProbability = atmConfig != null ? atmConfig.MonthlyFrequency / 30.0 : 0;
        var branchDailyProbability = branchDepositConfig != null ? branchDepositConfig.MonthlyFrequency / 30.0 : 0;

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

        var recurringPayments = BuildRecurringPayments(rng, profile, accountStart, hasHomeLoan);

        foreach (var rp in recurringPayments.Where(r => r.IsScheduled))
        {
            schedules.Add(new ScheduledPaymentSeed(
                account.Id, rp.PayeeName, rp.Amount, rp.Description, rp.Frequency, rp.FirstDate));
        }

        var runningBalance = openingDeposit;
        var nextPayDay = FindFirstPayDay(rng, accountStart, persona.IncomeFrequency);
        var current = accountStart.AddDays(1);
        var dayEnd = now.AddDays(-1);

        // Generate holiday periods for this customer
        var holidays = GenerateHolidays(rng, persona, accountStart, now);

        // Merge guaranteed holidays for spotlight customers (always on-trip / just-booked)
        var guaranteed = GetGuaranteedHolidays(
            profile.Customer.FirstName, profile.Customer.LastName, now);
        foreach (var gh in guaranteed)
        {
            // Only add if it doesn't overlap with an existing random holiday
            if (!holidays.Any(h => HolidaysOverlap(h, gh)))
                holidays.Add(gh);
        }

        var holidayBookingsGenerated = new HashSet<int>();

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

            // === HOLIDAY BOOKINGS (travel agent, pre-departure) ===
            for (int hi = 0; hi < holidays.Count; hi++)
            {
                var holiday = holidays[hi];
                if (!holidayBookingsGenerated.Contains(hi) && current.Date >= holiday.BookingDate.Date)
                {
                    holidayBookingsGenerated.Add(hi);
                    if (runningBalance >= holiday.FlightCost)
                    {
                        var agent = TravelAgents[rng.Next(TravelAgents.Length)];
                        var bookTime = current.Date.AddHours(rng.Next(10, 20)).AddMinutes(rng.Next(0, 60));
                        if (bookTime <= now)
                        {
                            txns.Add(MakeTxn(account.Id, -holiday.FlightCost,
                                $"{agent} - {holiday.Destination.Name.ToUpperInvariant()}",
                                TransactionType.Withdrawal, bookTime));
                            runningBalance -= holiday.FlightCost;
                        }
                    }
                }
            }

            var onHoliday = IsOnHoliday(holidays, current);

            // === DISCRETIONARY SPENDING (suppressed during holidays) ===
            if (!onHoliday)
            {
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
            }

            // === ATM WITHDRAWALS (suppressed during holidays — customer is overseas) ===
            if (!onHoliday && atmConfig != null && rng.NextDouble() < atmDailyProbability)
            {
                var atmAmount = RoundToNearest(RandomDecimal(rng, atmConfig.AmountMin, atmConfig.AmountMax), 20m);
                if (atmAmount > 0 && runningBalance >= atmAmount)
                {
                    var atmTime = current.Date.AddHours(rng.Next(8, 21)).AddMinutes(rng.Next(0, 60));
                    if (atmTime <= now)
                    {
                        // 70% use a home ATM, 30% use a random ATM
                        var atm = rng.NextDouble() < 0.70
                            ? homeAtms[rng.Next(homeAtms.Count)]
                            : atms[rng.Next(atms.Count)];
                        var atmTxn = MakeTxn(account.Id, -atmAmount,
                            $"ATM WITHDRAWAL - {atm.LocationName}",
                            TransactionType.Withdrawal, atmTime);
                        atmTxn.AtmId = atm.Id;
                        txns.Add(atmTxn);
                        runningBalance -= atmAmount;
                    }
                }
            }

            // === BRANCH DEPOSITS (suppressed during holidays) ===
            if (!onHoliday && branchDepositConfig != null && rng.NextDouble() < branchDailyProbability)
            {
                var depositAmount = RoundToNearest(RandomDecimal(rng, branchDepositConfig.AmountMin, branchDepositConfig.AmountMax), 10m);
                if (depositAmount > 0)
                {
                    var depositTime = current.Date.AddHours(rng.Next(9, 16)).AddMinutes(rng.Next(0, 60));
                    if (depositTime <= now)
                    {
                        // 80% at home branch, 20% at a random branch
                        var branch = rng.NextDouble() < 0.80
                            ? homeBranch
                            : branches[rng.Next(branches.Count)];
                        var branchTxn = MakeTxn(account.Id, depositAmount,
                            $"BRANCH DEPOSIT - {branch.Name}",
                            TransactionType.Deposit, depositTime);
                        branchTxn.BranchId = branch.Id;
                        txns.Add(branchTxn);
                        runningBalance += depositAmount;
                    }
                }
            }

            // === FOREIGN SPENDING (during holidays) ===
            if (onHoliday)
            {
                var holiday = GetCurrentHoliday(holidays, current)!;
                // 2–5 foreign transactions per day while on holiday
                var foreignSpendCount = rng.Next(2, 6);
                for (int fs = 0; fs < foreignSpendCount; fs++)
                {
                    var spendTime = current.Date.AddHours(rng.Next(8, 22)).AddMinutes(rng.Next(0, 60));
                    if (spendTime > now) break;
                    var (txn, totalAud) = PickForeignSpend(rng, account.Id, holiday, persona, spendTime);
                    if (totalAud > 0 && runningBalance >= totalAud)
                    {
                        txns.Add(txn);
                        runningBalance -= totalAud;
                    }
                }
            }

            // === OCCASIONAL LARGE EXPENSES (car repair, dental, medical, vet) — suppressed during holidays ===
            if (!onHoliday && rng.NextDouble() < 0.004) // ~1.5 times per year
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

    /// <summary>
    /// Generate historical transactions for a home loan: drawdown, monthly interest charges,
    /// PMT-based repayments, and (when applicable) offset account deposits.
    /// Uses <see cref="HomeLoanInterestCalculator"/> static methods to ensure seed data
    /// matches the real nightly interest accrual logic exactly.
    /// When <paramref name="offsetIsEveryday"/> is true, the offset account IS the everyday
    /// account and its balance is derived from the everyday transaction history.
    /// </summary>
    private static (List<Transaction> Txns, ScheduledPaymentSeed Schedule) GenerateHomeLoanHistory(
        Random rng, Account homeLoan, Account offset, Account everydayAccount,
        HomeLoanSeedConfig config, decimal loanAmount, decimal interestRate,
        CustomerProfile profile, DateTime now,
        bool offsetIsEveryday, Dictionary<DateOnly, decimal>? offsetMonthlyBalances)
    {
        var txns = new List<Transaction>();
        var accountStart = profile.Customer.CreatedAt;

        // Loan drawdown — sets initial debt
        txns.Add(MakeTxn(homeLoan.Id, -loanAmount, "LOAN DRAWDOWN",
            TransactionType.Deposit, accountStart));

        // When offset is NOT the everyday account, seed a standalone opening deposit + top-ups.
        // When it IS the everyday account, salary/spending already flow through it.
        decimal manualOffsetBalance = 0;
        if (!offsetIsEveryday)
        {
            var initialOffset = Math.Round(RandomDecimal(rng, config.OffsetMin, config.OffsetMax) / 100m) * 100m;
            txns.Add(MakeTxn(offset.Id, initialOffset, "OPENING DEPOSIT",
                TransactionType.Deposit, accountStart.AddHours(1)));
            manualOffsetBalance = initialOffset;
        }

        // Monthly repayment via the same PMT formula the calculator uses
        var monthlyRepayment = HomeLoanInterestCalculator.CalculateMonthlyRepayment(
            loanAmount, interestRate, config.TermMonths);

        // Track loan balance (negative = debt) — mirrors how the real system tracks Account.Balance.
        var loanBalance = -loanAmount;

        var loanStartDay = DateOnly.FromDateTime(accountStart);
        var startMonth = new DateOnly(accountStart.Year, accountStart.Month, 1);
        var firstPostingMonth = startMonth.AddMonths(1);
        var endDate = DateOnly.FromDateTime(now.AddDays(-1));

        // Average monthly offset top-up (only for separate-offset customers)
        var baseOffsetTopUp = !offsetIsEveryday
            ? profile.Persona.Name switch
            {
                "Established Professional" => RandomDecimal(rng, 500m, 2000m),
                "Young Family" => RandomDecimal(rng, 200m, 800m),
                "Young Professional" => RandomDecimal(rng, 100m, 500m),
                _ => 0m,
            }
            : 0m;

        // Track cumulative repayments debited from the offset (when it's the everyday account)
        // so we can adjust its balance for interest calculations.
        var cumulativeOffsetDebits = 0m;

        var postingMonth = firstPostingMonth;
        while (postingMonth <= endDate && loanBalance < 0)
        {
            var accrualMonth = postingMonth.AddMonths(-1);
            var daysInAccrualMonth = DateTime.DaysInMonth(accrualMonth.Year, accrualMonth.Month);
            var accrualDays = accrualMonth == startMonth
                ? daysInAccrualMonth - loanStartDay.Day + 1
                : daysInAccrualMonth;

            // Determine offset balance for interest calculation
            decimal offsetBalance;
            if (offsetIsEveryday && offsetMonthlyBalances != null)
            {
                // Use actual offset balance from everyday transactions, minus loan debits already made
                var everydayBal = GetLastBalanceAtOrBefore(offsetMonthlyBalances, accrualMonth);
                offsetBalance = Math.Max(0, everydayBal - cumulativeOffsetDebits);
            }
            else
            {
                offsetBalance = manualOffsetBalance;
            }

            // Daily interest using the SAME formula as the real HomeLoanInterestCalculator
            var principal = Math.Max(0, -loanBalance);
            var dailyInterest = HomeLoanInterestCalculator.CalculateDailyInterest(
                principal, offsetBalance, interestRate);
            var monthlyInterest = Math.Round(dailyInterest * accrualDays, 2);

            var postDate = new DateTime(postingMonth.Year, postingMonth.Month, postingMonth.Day,
                9, 0, 0, DateTimeKind.Utc);

            if (monthlyInterest != 0)
            {
                txns.Add(MakeTxn(homeLoan.Id, monthlyInterest,
                    $"Interest Charged — {accrualMonth:MMMM yyyy}",
                    TransactionType.Interest, postDate));
                loanBalance += monthlyInterest;
            }

            // Repayment: debit everyday account (offset or transaction), credit home loan
            var repaymentDate = postDate.AddHours(1);
            txns.Add(MakeTxn(homeLoan.Id, monthlyRepayment,
                "HOME LOAN REPAYMENT", TransactionType.Repayment, repaymentDate));
            txns.Add(MakeTxn(everydayAccount.Id, -monthlyRepayment,
                "HOME LOAN REPAYMENT", TransactionType.Repayment, repaymentDate));
            loanBalance += monthlyRepayment;

            if (offsetIsEveryday)
                cumulativeOffsetDebits += monthlyRepayment;

            // Offset top-ups only when offset is a separate account
            if (!offsetIsEveryday && baseOffsetTopUp > 0 && rng.NextDouble() < 0.6)
            {
                var topUp = Math.Round(baseOffsetTopUp * RandomDecimal(rng, 0.5m, 1.5m), 2);
                var topUpDate = postDate.AddDays(rng.Next(1, 15));
                if (DateOnly.FromDateTime(topUpDate) <= endDate)
                {
                    txns.Add(MakeTxn(offset.Id, topUp, "TRANSFER FROM EVERYDAY",
                        TransactionType.Transfer, topUpDate));
                    txns.Add(MakeTxn(everydayAccount.Id, -topUp, "TRANSFER TO OFFSET",
                        TransactionType.Transfer, topUpDate));
                    manualOffsetBalance += topUp;
                }
            }

            postingMonth = postingMonth.AddMonths(1);
        }

        // Scheduled repayment for future months
        var nextDue = postingMonth > endDate ? postingMonth : postingMonth.AddMonths(1);
        var schedule = new ScheduledPaymentSeed(
            everydayAccount.Id, "HOME LOAN REPAYMENT", monthlyRepayment,
            "HOME LOAN REPAYMENT", ScheduleFrequency.Monthly,
            new DateTime(nextDue.Year, nextDue.Month, nextDue.Day, 10, 0, 0, DateTimeKind.Utc),
            homeLoan.Id);

        return (txns, schedule);
    }

    /// <summary>
    /// Find the last balance snapshot at or before the given month.
    /// </summary>
    private static decimal GetLastBalanceAtOrBefore(Dictionary<DateOnly, decimal> snapshots, DateOnly month)
    {
        var best = 0m;
        foreach (var kvp in snapshots)
        {
            if (kvp.Key <= month)
                best = kvp.Value;
        }
        return best;
    }

    /// <summary>
    /// Generate savings account transaction history: opening deposit, monthly transfers
    /// from the everyday account, and occasional withdrawals back to everyday.
    /// Both legs of each transfer are generated (debit + credit).
    /// Withdrawal frequency is persona-dependent and determines whether the customer
    /// earns bonus interest for that month (Australian-style conditional rate).
    /// </summary>
    private static List<Transaction> GenerateSavingsHistory(
        Random rng, Account savings, Account everydayAccount,
        SavingsSeedConfig config, CustomerProfile profile, DateTime now)
    {
        var txns = new List<Transaction>();
        var accountStart = profile.Customer.CreatedAt;
        var persona = profile.Persona;
        var dayEnd = now.AddDays(-1);

        // Opening deposit (external funds, not from everyday account)
        var openingDeposit = RandomDecimal(rng, config.OpeningMin, config.OpeningMax);
        txns.Add(MakeTxn(savings.Id, openingDeposit, "Opening deposit",
            TransactionType.Deposit, accountStart));
        var savingsBalance = openingDeposit;

        // Walk through months from account start
        var firstMonth = new DateOnly(accountStart.Year, accountStart.Month, 1).AddMonths(1);
        var endMonth = new DateOnly(dayEnd.Year, dayEnd.Month, 1);
        var currentMonth = firstMonth;

        // Determine payday schedule to time transfers appropriately
        var nextPayDay = FindFirstPayDay(rng, accountStart, persona.IncomeFrequency);
        while (nextPayDay < accountStart.AddMonths(1))
            nextPayDay = AdvanceByIncomeFrequency(nextPayDay, persona.IncomeFrequency);

        while (currentMonth <= endMonth)
        {
            var monthDate = new DateTime(currentMonth.Year, currentMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // === MONTHLY DEPOSIT (transfer from everyday → savings) ===
            if (rng.NextDouble() < config.TransferLikelihood)
            {
                var transferAmount = RandomDecimal(rng, config.MonthlyTransferMin, config.MonthlyTransferMax);
                // Transfer happens 1-3 days after start of month (around payday)
                var transferDay = rng.Next(1, 4);
                var daysInMonth = DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);
                transferDay = Math.Min(transferDay, daysInMonth);
                var transferDate = monthDate.AddDays(transferDay).AddHours(rng.Next(10, 15)).AddMinutes(rng.Next(0, 60));

                if (transferDate <= dayEnd)
                {
                    txns.Add(MakeTxn(savings.Id, transferAmount, "Transfer from Everyday",
                        TransactionType.Transfer, transferDate));
                    txns.Add(MakeTxn(everydayAccount.Id, -transferAmount, "Transfer to Savings",
                        TransactionType.Transfer, transferDate));
                    savingsBalance += transferAmount;
                }
            }

            // === OCCASIONAL WITHDRAWAL (savings → everyday) ===
            if (rng.NextDouble() < config.WithdrawalLikelihood)
            {
                var withdrawalAmount = RandomDecimal(rng, config.WithdrawalMin, config.WithdrawalMax);
                withdrawalAmount = Math.Min(withdrawalAmount, savingsBalance * 0.8m); // Don't drain the account
                withdrawalAmount = Math.Round(withdrawalAmount, 2);

                if (withdrawalAmount >= 10m && savingsBalance > withdrawalAmount)
                {
                    var reasons = SavingsWithdrawalReasons.GetValueOrDefault(persona.Name,
                        ["TRANSFER"]);
                    var reason = reasons[rng.Next(reasons.Length)];
                    var withdrawDay = rng.Next(5, 25);
                    var daysInMonth = DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);
                    withdrawDay = Math.Min(withdrawDay, daysInMonth);
                    var withdrawDate = monthDate.AddDays(withdrawDay).AddHours(rng.Next(10, 16)).AddMinutes(rng.Next(0, 60));

                    if (withdrawDate <= dayEnd)
                    {
                        txns.Add(MakeTxn(savings.Id, -withdrawalAmount,
                            $"Transfer to Everyday - {reason}",
                            TransactionType.Transfer, withdrawDate));
                        txns.Add(MakeTxn(everydayAccount.Id, withdrawalAmount,
                            "Transfer from Savings",
                            TransactionType.Transfer, withdrawDate));
                        savingsBalance -= withdrawalAmount;
                    }
                }
            }

            currentMonth = currentMonth.AddMonths(1);
        }

        return txns;
    }

    private static List<RecurringPayment> BuildRecurringPayments(
        Random rng, CustomerProfile profile, DateTime accountStart, bool hasHomeLoan)
    {
        var payments = new List<RecurringPayment>();
        var persona = profile.Persona;

        // Rent (skip if customer has a home loan — they're a homeowner)
        if (!hasHomeLoan && rng.NextDouble() < persona.RentLikelihood && persona.RentRange.Max > 0)
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

    private static DateTime FindFirstPayDay(Random rng, DateTime accountStart, IncomeFrequency freq)
    {
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
                PayeeAccountId = s.PayeeAccountId,
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

    /// <summary>
    /// Seed registered holiday records for spotlight customers.
    /// Grace Turner and Gabriel White "told the bank" about their trips.
    /// Chloe Martin and Ethan Ross did NOT register, which will trigger travel nudge signals.
    /// </summary>
    private static void SeedCustomerHolidays(BankDbContext db, DateTime now)
    {
        // Grace Turner — registered her Fiji family holiday before departing
        var grace = db.Customers.First(c => c.FirstName == "Grace" && c.LastName == "Turner");
        var graceReturn = now.AddDays(-2);
        var graceDeparture = graceReturn.AddDays(-9);
        db.CustomerHolidays.Add(new CustomerHoliday
        {
            CustomerId = grace.Id,
            Destination = "Fiji",
            StartDate = DateOnly.FromDateTime(graceDeparture),
            EndDate = DateOnly.FromDateTime(graceReturn),
        });

        // Gabriel White — registered his upcoming Bali trip when he booked
        var gabriel = db.Customers.First(c => c.FirstName == "Gabriel" && c.LastName == "White");
        var gabrielDeparture = now.AddDays(10);
        var gabrielReturn = gabrielDeparture.AddDays(14);
        db.CustomerHolidays.Add(new CustomerHoliday
        {
            CustomerId = gabriel.Id,
            Destination = "Bali",
            StartDate = DateOnly.FromDateTime(gabrielDeparture),
            EndDate = DateOnly.FromDateTime(gabrielReturn),
        });

        // Chloe Martin — currently in Japan but did NOT register (triggers FOREIGN_SPEND_NO_HOLIDAY)
        // Ethan Ross — booked Europe trip but did NOT register (triggers FLIGHT_BOOKING_DETECTED)

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

    /// <summary>
    /// Generate holiday periods for a customer based on their persona.
    /// Holidays are spread across the customer's tenure, avoiding the first 60 days
    /// and the last 7 days of history. Customers may have 0–2 holidays.
    /// </summary>
    private static List<HolidayPeriod> GenerateHolidays(
        Random rng, PersonaTemplate persona, DateTime accountStart, DateTime now)
    {
        var holidays = new List<HolidayPeriod>();
        if (!HolidayConfigs.TryGetValue(persona.Name, out var config))
            return holidays;

        var tenureDays = (int)(now - accountStart).TotalDays;
        if (tenureDays < 90) return holidays; // too new for a holiday

        // Roll for first holiday
        if (rng.NextDouble() >= config.Probability) return holidays;

        var earliest = accountStart.AddDays(60);
        var latest = now.AddDays(-7);

        var holiday1 = BuildHolidayPeriod(rng, config, earliest, latest);
        if (holiday1 is not null) holidays.Add(holiday1);

        // Long-tenure customers may get a second holiday (half the base probability)
        if (tenureDays > 365 && rng.NextDouble() < config.Probability * 0.5)
        {
            var holiday2 = BuildHolidayPeriod(rng, config, earliest, latest);
            if (holiday2 is not null && !HolidaysOverlap(holiday1!, holiday2))
                holidays.Add(holiday2);
        }

        return holidays;
    }

    private static HolidayPeriod? BuildHolidayPeriod(
        Random rng, HolidayConfig config, DateTime earliest, DateTime latest)
    {
        var duration = rng.Next(config.MinDays, config.MaxDays + 1);
        var windowDays = (int)(latest - earliest).TotalDays - duration - 30; // 30 days buffer for booking
        if (windowDays < 1) return null;

        var departure = earliest.AddDays(rng.Next(30, windowDays + 30));
        var destIndex = config.EligibleDestinations[rng.Next(config.EligibleDestinations.Length)];
        var destination = Destinations[destIndex];
        var flightCost = RandomDecimal(rng, config.FlightMin, config.FlightMax);
        var bookingDate = departure.AddDays(-rng.Next(14, 31));

        return new HolidayPeriod(departure, duration, destination, flightCost, bookingDate);
    }

    private static bool HolidaysOverlap(HolidayPeriod a, HolidayPeriod b)
    {
        var aEnd = a.DepartureDate.AddDays(a.DurationDays);
        var bEnd = b.DepartureDate.AddDays(b.DurationDays);
        // Also require 14-day gap between holidays
        return a.DepartureDate.AddDays(-14) < bEnd && b.DepartureDate.AddDays(-14) < aEnd;
    }

    private static bool IsOnHoliday(List<HolidayPeriod> holidays, DateTime date)
    {
        foreach (var h in holidays)
        {
            if (date.Date >= h.DepartureDate.Date && date.Date < h.DepartureDate.AddDays(h.DurationDays).Date)
                return true;
        }
        return false;
    }

    private static HolidayPeriod? GetCurrentHoliday(List<HolidayPeriod> holidays, DateTime date)
    {
        foreach (var h in holidays)
        {
            if (date.Date >= h.DepartureDate.Date && date.Date < h.DepartureDate.AddDays(h.DurationDays).Date)
                return h;
        }
        return null;
    }

    private static Transaction MakeForeignTxn(
        int accountId, decimal audAmount, string description,
        DateTime createdAt, string currency, decimal originalAmount,
        decimal exchangeRate, decimal feeAmount) =>
        new()
        {
            AccountId = accountId,
            Amount = audAmount,
            Description = description,
            TransactionType = TransactionType.Withdrawal,
            Status = TransactionStatus.Settled,
            SettledAt = createdAt,
            CreatedAt = createdAt,
            OriginalCurrency = currency,
            OriginalAmount = originalAmount,
            ExchangeRate = exchangeRate,
            FeeAmount = feeAmount,
        };

    /// <summary>
    /// Pick a foreign spend during a holiday: choose a destination merchant,
    /// convert to AUD using the exchange rate (with small daily variance), and
    /// calculate the 3% international transaction fee.
    /// </summary>
    private static (Transaction Txn, decimal TotalAud) PickForeignSpend(
        Random rng, int accountId, HolidayPeriod holiday, PersonaTemplate persona, DateTime spendTime)
    {
        var dest = holiday.Destination;
        var merchant = dest.Merchants[rng.Next(dest.Merchants.Length)];

        var foreignAmount = RandomDecimal(rng, merchant.Min, merchant.Max);

        // Persona-based scaling: budget travellers spend less
        if (persona.Name is "Student" or "Modest Retiree")
            foreignAmount = Math.Round(foreignAmount * RandomDecimal(rng, 0.4m, 0.7m), 2);
        else if (persona.Name is "Zero-Hours Worker" or "Single Parent")
            foreignAmount = Math.Round(foreignAmount * RandomDecimal(rng, 0.5m, 0.8m), 2);

        // Daily FX variance: ±2% from base rate
        var fxVariance = rng.NextDouble() * 0.04 - 0.02;
        var effectiveRate = ForeignTransactionCalculator.ApplyFxVariance(dest.ExchangeRate, fxVariance);

        // Convert foreign amount to AUD
        var audAmount = ForeignTransactionCalculator.ConvertToAud(foreignAmount, effectiveRate);
        var fee = ForeignTransactionCalculator.CalculateFee(audAmount, InternationalFeeRate);
        var totalAud = audAmount + fee;

        var txn = MakeForeignTxn(
            accountId, -totalAud, merchant.Name,
            spendTime, dest.Currency, foreignAmount, effectiveRate, fee);

        return (txn, totalAud);
    }

    private static decimal RandomDecimal(Random rng, decimal min, decimal max) =>
        Math.Round(min + (max - min) * (decimal)rng.NextDouble(), 2);

    /// <summary>ATM amounts are rounded to the nearest denomination (e.g., $20 notes).</summary>
    private static decimal RoundToNearest(decimal value, decimal denomination) =>
        Math.Max(denomination, Math.Round(value / denomination) * denomination);

    private static List<Branch> SeedBranches(BankDbContext db)
    {
        var branches = new List<Branch>();
        foreach (var seed in BranchSeeds)
        {
            var branch = new Branch
            {
                Name = seed.Name,
                Address = seed.Address,
                Suburb = seed.Suburb,
                State = seed.State,
                Postcode = seed.Postcode,
                Latitude = seed.Lat,
                Longitude = seed.Lng,
            };
            branches.Add(branch);
        }
        db.Branches.AddRange(branches);
        db.SaveChanges();
        return branches;
    }

    private static List<Atm> SeedAtms(BankDbContext db, List<Branch> branches)
    {
        var atms = new List<Atm>();
        foreach (var seed in AtmSeeds)
        {
            var atm = new Atm
            {
                LocationName = seed.LocationName,
                Address = seed.Address,
                Suburb = seed.Suburb,
                State = seed.State,
                Postcode = seed.Postcode,
                Latitude = seed.Lat,
                Longitude = seed.Lng,
                BranchId = seed.BranchIndex.HasValue ? branches[seed.BranchIndex.Value].Id : null,
            };
            atms.Add(atm);
        }
        db.Atms.AddRange(atms);
        db.SaveChanges();
        return atms;
    }

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

    // ────────────────────────────────────────────────────────────────────────
    //  APP Fraud Scenario — "Operation Swift"
    //
    //  4 mule accounts form a layered scam network. 5 existing customers are
    //  victims who are socially engineered into making payments to the collector
    //  mule. Money cascades through the network with commission taken at each
    //  hop. The transactions are completely independent — no linking IDs.
    //  Neo4j Cypher queries discover the hidden pattern through graph traversal.
    // ────────────────────────────────────────────────────────────────────────

    private static void SeedFraudScenario(BankDbContext db, DateTime now, ref int accountNumber)
    {
        var rng = new Random(9999); // deterministic but separate from main seed
        var today = now.Date;

        // ── Mule definitions ──────────────────────────────────────────────
        //  Mix of burner (new, sparse) and established (normal-looking) accounts.
        var muleDefinitions = new[]
        {
            new {
                FirstName = "Marcus", LastName = "Webb", Persona = "Zero-Hours Worker",
                Age = 25, DaysOld = 21, // 🔥 Burner — opened ~3 weeks ago
                Role = "Collector",
                HasCamouflage = false,
            },
            new {
                FirstName = "Jade", LastName = "Thornton", Persona = "Young Professional",
                Age = 30, DaysOld = 240, // 🏠 Established — ~8 months old
                Role = "Layer1",
                HasCamouflage = true,
            },
            new {
                FirstName = "Ryan", LastName = "Kovac", Persona = "Zero-Hours Worker",
                Age = 28, DaysOld = 14, // 🔥 Burner — opened ~2 weeks ago
                Role = "Layer2",
                HasCamouflage = false,
            },
            new {
                FirstName = "Priya", LastName = "Desai", Persona = "Young Professional",
                Age = 33, DaysOld = 120, // 🏠 Semi-established — ~4 months old
                Role = "CashOut",
                HasCamouflage = true,
            },
        };

        // ── Create mule customers & accounts ──────────────────────────────
        var muleAccounts = new Dictionary<string, Account>(); // keyed by Role

        foreach (var m in muleDefinitions)
        {
            var createdAt = today.AddDays(-m.DaysOld);
            var dob = DateOnly.FromDateTime(today.AddYears(-m.Age));
            var customer = new Customer
            {
                FirstName = m.FirstName,
                LastName = m.LastName,
                Email = $"{m.FirstName.ToLower()}.{m.LastName.ToLower()}@email.com.au",
                Phone = $"04{rng.Next(10, 100):00} {rng.Next(100, 1000):000} {rng.Next(100, 1000):000}",
                DateOfBirth = dob,
                CreatedAt = createdAt,
                Persona = m.Persona,
            };
            db.Customers.Add(customer);
            db.SaveChanges();

            var account = new Account
            {
                CustomerId = customer.Id,
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

            muleAccounts[m.Role] = account;

            // ── Camouflage activity for established mules ─────────────────
            if (m.HasCamouflage)
                SeedMuleCamouflage(db, account, rng, createdAt, today);
        }

        // ── Victim payments into the scam network ─────────────────────────
        //  Each victim is an existing customer who gets tricked into paying
        //  the collector mule (Marcus). The descriptions look like plausible
        //  payment reasons — the victims believe these are legitimate.

        var victimSpecs = new[]
        {
            new { FirstName = "Gabriel", LastName = "White",  Amount = 4800m, DaysAgo = 12,
                  Description = "ATO PAYMENT - OVERDUE TAX NOTICE" },
            new { FirstName = "Margaret", LastName = "Kelly",  Amount = 2200m, DaysAgo = 10,
                  Description = "SECURE ACCOUNT TRANSFER" },
            new { FirstName = "Chloe",    LastName = "Martin", Amount = 5500m, DaysAgo = 7,
                  Description = "MEDICAL EMERGENCY TRANSFER" },
            new { FirstName = "Grace",    LastName = "Turner", Amount = 3100m, DaysAgo = 5,
                  Description = "INVOICE PAYMENT - PLUMBING SERVICES" },
            new { FirstName = "Noah",     LastName = "Patel",  Amount = 1200m, DaysAgo = 3,
                  Description = "EQUIPMENT BOND - NEW STARTER KIT" },
        };

        var collector = muleAccounts["Collector"];
        var layer1    = muleAccounts["Layer1"];
        var layer2    = muleAccounts["Layer2"];
        var cashOut   = muleAccounts["CashOut"];

        // Commission rates vary slightly per transaction to avoid exact-match detection
        var commissionRates = new[]
        {
            new { ToLayer1 = 0.078m, ToLayer2 = 0.052m, ToCashOut = 0.031m },
            new { ToLayer1 = 0.082m, ToLayer2 = 0.048m, ToCashOut = 0.029m },
            new { ToLayer1 = 0.075m, ToLayer2 = 0.055m, ToCashOut = 0.033m },
            new { ToLayer1 = 0.085m, ToLayer2 = 0.047m, ToCashOut = 0.028m },
            new { ToLayer1 = 0.079m, ToLayer2 = 0.051m, ToCashOut = 0.032m },
        };

        var txns = new List<Transaction>();

        for (int i = 0; i < victimSpecs.Length; i++)
        {
            var v = victimSpecs[i];
            var commission = commissionRates[i];

            // Find the victim's everyday account (Transaction, or Offset when offset-as-everyday)
            var victim = db.Customers.First(c => c.FirstName == v.FirstName && c.LastName == v.LastName);
            var victimAccount = db.Accounts
                .Where(a => a.CustomerId == victim.Id
                    && (a.AccountType == AccountType.Transaction || a.AccountType == AccountType.Offset)
                    && a.IsActive)
                .OrderBy(a => a.AccountType) // Transaction (2) before Offset (3) - prefer Transaction
                .First();

            var paymentTime = today.AddDays(-v.DaysAgo).AddHours(10).AddMinutes(rng.Next(0, 120));

            // ── Leg 1: Victim pays collector ──────────────────────────────
            // Both legs share the same CreatedAt (same-bank instant settlement)
            txns.Add(MakeTxn(victimAccount.Id, -v.Amount, v.Description,
                TransactionType.Transfer, paymentTime));
            txns.Add(MakeTxn(collector.Id, v.Amount, $"PAYMENT FROM {victim.FirstName.ToUpper()} {victim.LastName.ToUpper()}",
                TransactionType.Transfer, paymentTime));

            // ── Leg 2: Collector forwards to Layer 1 (hours later) ────────
            var amount1 = Math.Round(v.Amount * (1m - commission.ToLayer1), 2);
            var forwardTime1 = paymentTime.AddHours(rng.Next(2, 6)).AddMinutes(rng.Next(0, 60));
            txns.Add(MakeTxn(collector.Id, -amount1, $"TRANSFER TO {layer1.AccountNumber}",
                TransactionType.Transfer, forwardTime1));
            txns.Add(MakeTxn(layer1.Id, amount1, $"TRANSFER FROM {collector.AccountNumber}",
                TransactionType.Transfer, forwardTime1));

            // ── Leg 3: Layer 1 forwards to Layer 2 (hours later) ──────────
            var amount2 = Math.Round(amount1 * (1m - commission.ToLayer2), 2);
            var forwardTime2 = forwardTime1.AddHours(rng.Next(1, 4)).AddMinutes(rng.Next(0, 60));
            txns.Add(MakeTxn(layer1.Id, -amount2, $"TRANSFER TO {layer2.AccountNumber}",
                TransactionType.Transfer, forwardTime2));
            txns.Add(MakeTxn(layer2.Id, amount2, $"TRANSFER FROM {layer1.AccountNumber}",
                TransactionType.Transfer, forwardTime2));

            // ── Leg 4: Layer 2 forwards to Cash-out (next day) ────────────
            var amount3 = Math.Round(amount2 * (1m - commission.ToCashOut), 2);
            var forwardTime3 = forwardTime2.AddHours(rng.Next(3, 8)).AddMinutes(rng.Next(0, 60));
            txns.Add(MakeTxn(layer2.Id, -amount3, $"TRANSFER TO {cashOut.AccountNumber}",
                TransactionType.Transfer, forwardTime3));
            txns.Add(MakeTxn(cashOut.Id, amount3, $"TRANSFER FROM {layer2.AccountNumber}",
                TransactionType.Transfer, forwardTime3));

            // ── Cash-out: ATM withdrawal ──────────────────────────────────
            var withdrawAmount = RoundToNearest(amount3 - rng.Next(20, 80), 20m);
            var withdrawTime = forwardTime3.AddHours(rng.Next(1, 3));
            txns.Add(MakeTxn(cashOut.Id, -withdrawAmount, "ATM WITHDRAWAL",
                TransactionType.Withdrawal, withdrawTime));
        }

        db.Transactions.AddRange(txns);
        db.SaveChanges();

        // Recalculate balances for all affected accounts
        var affectedAccountIds = txns.Select(t => t.AccountId).Distinct().ToList();
        foreach (var accountId in affectedAccountIds)
        {
            var account = db.Accounts.Find(accountId)!;
            account.Balance = db.Transactions
                .Where(t => t.AccountId == accountId && t.Status == TransactionStatus.Settled)
                .Sum(t => t.Amount);
            db.Entry(account).State = EntityState.Modified;
        }
        db.SaveChanges();
    }

    /// <summary>
    /// Adds normal-looking transactions to an established mule account so it
    /// doesn't stand out from basic account-level monitoring.
    /// </summary>
    private static void SeedMuleCamouflage(
        BankDbContext db, Account account, Random rng, DateTime accountStart, DateTime today)
    {
        var txns = new List<Transaction>();
        var current = accountStart.AddDays(1);

        while (current < today)
        {
            // Fortnightly salary deposit
            if ((current - accountStart).Days % 14 == 0)
            {
                var salary = Math.Round(2200m + (decimal)(rng.NextDouble() * 800), 2);
                txns.Add(MakeTxn(account.Id, salary, "SALARY PAYMENT",
                    TransactionType.Deposit, current.Date.AddHours(9).AddMinutes(rng.Next(0, 30))));
            }

            // Occasional grocery / daily spending
            if (rng.NextDouble() < 0.25)
            {
                var groceryAmount = Math.Round(15m + (decimal)(rng.NextDouble() * 80), 2);
                string[] shops = ["WOOLWORTHS", "COLES", "ALDI", "IGA"];
                txns.Add(MakeTxn(account.Id, -groceryAmount,
                    shops[rng.Next(shops.Length)],
                    TransactionType.Withdrawal, current.Date.AddHours(12).AddMinutes(rng.Next(0, 480))));
            }

            // Monthly mobile bill
            if (current.Day == 15)
            {
                var mobile = Math.Round(45m + (decimal)(rng.NextDouble() * 30), 2);
                txns.Add(MakeTxn(account.Id, -mobile, "VODAFONE MOBILE",
                    TransactionType.DirectDebit, current.Date.AddHours(8)));
            }

            // Monthly rent (established mules)
            if (current.Day == 1 && current > accountStart.AddDays(14))
            {
                var rent = Math.Round(350m + (decimal)(rng.NextDouble() * 150), 2);
                txns.Add(MakeTxn(account.Id, -rent, "RENT PAYMENT - RAY WHITE",
                    TransactionType.DirectDebit, current.Date.AddHours(10)));
            }

            current = current.AddDays(1);
        }

        db.Transactions.AddRange(txns);
        db.SaveChanges();
    }

    // ── End of APP Fraud Scenario ─────────────────────────────────────────

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
        DateTime FirstBillingDate,
        int? PayeeAccountId = null);

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

    private sealed record HomeLoanSeedConfig(
        decimal LoanMin, decimal LoanMax,
        decimal RateMin, decimal RateMax,
        int TermMonths,
        double Likelihood,
        decimal OffsetMin, decimal OffsetMax);

    private sealed record SavingsSeedConfig(
        double Likelihood,
        decimal OpeningMin, decimal OpeningMax,
        decimal MonthlyTransferMin, decimal MonthlyTransferMax,
        double TransferLikelihood,
        double WithdrawalLikelihood,
        decimal WithdrawalMin, decimal WithdrawalMax);
}
