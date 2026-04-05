using System.Text.Json.Serialization;
using BudgetModel = MonthlyBudget.Api.MonthlyBudget;

namespace MonthlyBudget.Api;

public class Expense
{
    public const decimal MinAmount = 0.01m;
    public const decimal MaxAmount = 999999999m;

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CategoryId { get; set; }
    public string CategoryName => Category?.Name ?? "Brak kategorii";
    public int MonthlyBudgetId { get; set; }

    [JsonIgnore]
    public BudgetModel? MonthlyBudget { get; set; }

    [JsonIgnore]
    public Category? Category { get; set; }

    public static bool IsAmountValid(decimal amount)
    {
        return amount >= MinAmount && amount <= MaxAmount;
    }
}
