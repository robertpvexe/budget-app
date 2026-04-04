namespace MonthlyBudget.Web.Models;

public class BudgetDetailsResponse
{
    public int Id { get; set; }
    public string Month { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public List<ExpenseDto> Expenses { get; set; } = [];
    public decimal TotalExpenses { get; set; }
    public decimal Remaining { get; set; }
    public decimal Percentage { get; set; }
}
