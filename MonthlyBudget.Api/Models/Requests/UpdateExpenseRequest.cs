namespace MonthlyBudget.Api;

public class UpdateExpenseRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CategoryId { get; set; }
}
