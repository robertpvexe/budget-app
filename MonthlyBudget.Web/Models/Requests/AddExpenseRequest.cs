namespace MonthlyBudget.Web.Models.Requests;

public class AddExpenseRequest
{
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal Amount { get; set; }
    public int CategoryId { get; set; }
}
