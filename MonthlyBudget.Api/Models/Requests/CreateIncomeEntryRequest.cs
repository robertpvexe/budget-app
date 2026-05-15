namespace MonthlyBudget.Api;

public class CreateIncomeEntryRequest
{
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Description { get; set; }
}
