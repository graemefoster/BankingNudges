namespace BankOfGraeme.Api.Services;

public static class MerchantCategoryMapper
{
    private static readonly Dictionary<string, string[]> CategoryMerchants = new()
    {
        ["Groceries"] = ["WOOLWORTHS", "COLES", "ALDI", "IGA", "HARRIS FARM"],
        ["Fuel"] = ["BP ", "SHELL", "AMPOL", "7-ELEVEN FUEL"],
        ["Dining"] = ["MCDONALD", "GUZMAN", "SUSHI", "UBER EATS", "MENULOG", "COFFEE", "GLORIA JEAN", "BOOST JUICE"],
        ["Bars"] = ["HOTEL", "TAVERN", "BREWPUB", "COURTHOUSE"],
        ["Transport"] = ["UBER *TRIP", "DIDI", "OPAL", "PARKING"],
        ["Health"] = ["CHEMIST", "PHARMACY", "DENTAL", "MEDICAL", "DR "],
        ["Retail"] = ["BUNNINGS", "JB HI-FI", "KMART", "BIG W", "TARGET"],
        ["Utilities"] = ["AGL", "ORIGIN ENERGY", "SYDNEY WATER", "TELSTRA", "OPTUS", "VODAFONE"]
    };

    private static readonly Dictionary<string, string> MerchantDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        ["WOOLWORTHS"] = "woolworths.com.au",
        ["COLES"] = "coles.com.au",
        ["ALDI"] = "aldi.com.au",
        ["IGA"] = "iga.com.au",
        ["HARRIS FARM"] = "harrisfarm.com.au",
        ["BP "] = "bp.com.au",
        ["SHELL"] = "shell.com.au",
        ["AMPOL"] = "ampol.com.au",
        ["7-ELEVEN FUEL"] = "7eleven.com.au",
        ["MCDONALD"] = "mcdonalds.com.au",
        ["GUZMAN"] = "guzmanygomez.com",
        ["UBER EATS"] = "ubereats.com",
        ["MENULOG"] = "menulog.com.au",
        ["COFFEE"] = "coffee.com",
        ["GLORIA JEAN"] = "gloriajeanscoffees.com.au",
        ["BOOST JUICE"] = "boostjuice.com.au",
        ["UBER *TRIP"] = "uber.com",
        ["DIDI"] = "didiglobal.com",
        ["OPAL"] = "opal.com.au",
        ["BUNNINGS"] = "bunnings.com.au",
        ["JB HI-FI"] = "jbhifi.com.au",
        ["KMART"] = "kmart.com.au",
        ["BIG W"] = "bigw.com.au",
        ["TARGET"] = "target.com.au",
        ["AGL"] = "agl.com.au",
        ["ORIGIN ENERGY"] = "originenergy.com.au",
        ["SYDNEY WATER"] = "sydneywater.com.au",
        ["TELSTRA"] = "telstra.com.au",
        ["OPTUS"] = "optus.com.au",
        ["VODAFONE"] = "vodafone.com.au",
    };

    public static IReadOnlyList<string> AllCategories { get; } =
        CategoryMerchants.Keys.Append("Other").ToList().AsReadOnly();

    public static string Categorise(string description)
    {
        var upper = description.ToUpperInvariant();
        foreach (var (category, merchants) in CategoryMerchants)
        {
            if (merchants.Any(m => upper.Contains(m)))
                return category;
        }
        return "Other";
    }

    /// <summary>
    /// Returns a logo URL for the first recognised merchant keyword in the description,
    /// or null when no known merchant is matched.
    /// </summary>
    public static string? GetMerchantLogoUrl(string description)
    {
        var upper = description.ToUpperInvariant();
        foreach (var (keyword, domain) in MerchantDomains)
        {
            if (upper.Contains(keyword))
                return $"https://icons.duckduckgo.com/ip3/{domain}.ico";
        }
        return null;
    }

    public static string[] GetMerchantKeywords(string category)
    {
        return CategoryMerchants.TryGetValue(category, out var merchants) ? merchants : [];
    }
}
