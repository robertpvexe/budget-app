namespace MonthlyBudget.Api;

public static class CategoryDefaults
{
    public const int NoCategoryId = 0;
    public const string NoCategoryName = "Brak kategorii";

    public static readonly string[] StarterCategoryNames =
    [
        NoCategoryName,
        "Jedzenie",
        "Napoje",
        "Rachunki",
        "Subskrypcje",
        "Inne"
    ];

    public static bool IsNoCategoryName(string? name)
    {
        return string.Equals(name?.Trim(), NoCategoryName, StringComparison.CurrentCultureIgnoreCase);
    }
}
