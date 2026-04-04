namespace MonthlyBudget.Web.Models.Requests;

public class CreateMonthlyBudgetRequest
{
    public string Month { get; set; } = string.Empty;
    public decimal Income { get; set; }
}
