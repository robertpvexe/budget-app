namespace MonthlyBudget.Web.Models;

public class BudgetDetailsResponse
{
    public int Id { get; set; }
    public string Month { get; set; } = string.Empty;
    public List<ExpenseDto> Expenses { get; set; } = [];
    public decimal TotalExpenses { get; set; }
}
