namespace MonthlyBudget.Web.Models;

public class ExpenseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int MonthlyBudgetId { get; set; }
}
