namespace MonthlyBudget.Api;

public class MonthlyBudget
{
    public int Id { get; set; }
    public string Month { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public List<Expense> Expenses { get; set; } = [];

    public decimal GetTotalExpenses()
    {
        return Expenses.Sum(expense => expense.Amount);
    }

    public decimal GetRemainingAmount()
    {
        return Income - GetTotalExpenses();
    }

    public decimal GetExpensePercentage()
    {
        if (Income == 0)
        {
            return 0;
        }

        return (GetTotalExpenses() / Income) * 100;
    }
}
