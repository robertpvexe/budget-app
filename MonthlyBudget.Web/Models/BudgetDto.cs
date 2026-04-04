namespace MonthlyBudget.Web.Models;

public class BudgetDto
{
    public int Id { get; set; }
    public string Month { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public List<ExpenseDto> Expenses { get; set; } = [];
}
