namespace MonthlyBudget.Api;

public class IncomeEntry
{
    public const decimal MinAmount = 0.01m;
    public const int MaxDescriptionLength = 200;

    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;

    public static bool IsAmountValid(decimal amount)
    {
        return amount > 0;
    }
}
