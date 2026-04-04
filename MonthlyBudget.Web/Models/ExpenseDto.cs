namespace MonthlyBudget.Web.Models;

public class ExpenseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public int MonthlyBudgetId { get; set; }
}
