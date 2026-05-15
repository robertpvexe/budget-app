namespace MonthlyBudget.Api;

public class BudgetDetailsResponse
{
    public int Id { get; set; }
    public string Month { get; set; } = string.Empty;
    public List<Expense> Expenses { get; set; } = [];
    public decimal TotalExpenses { get; set; }
}
