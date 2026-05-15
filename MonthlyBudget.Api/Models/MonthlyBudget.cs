namespace MonthlyBudget.Api;

public class MonthlyBudget
{
    public int Id { get; set; }
    public string Month { get; set; } = string.Empty;
    public List<Expense> Expenses { get; set; } = [];

    public decimal GetTotalExpenses()
    {
        return Expenses.Sum(expense => expense.Amount);
    }
}
