using System.Text.Json.Serialization;
using BudgetModel = MonthlyBudget.Api.Models.MonthlyBudget;

namespace MonthlyBudget.Api.Models;

public class Expense
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public int MonthlyBudgetId { get; set; }

    [JsonIgnore]
    public BudgetModel? MonthlyBudget { get; set; }
}
