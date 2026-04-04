namespace MonthlyBudget.Web.Models.Requests;

public class AddExpenseRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
}
