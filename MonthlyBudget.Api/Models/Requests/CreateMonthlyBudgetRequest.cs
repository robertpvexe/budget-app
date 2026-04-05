using System.ComponentModel.DataAnnotations;

namespace MonthlyBudget.Api;

public class CreateMonthlyBudgetRequest
{
    [Required]
    [RegularExpression(@"^\d{4}-\d{2}$")]
    public string Month { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "999999999")]
    public decimal Income { get; set; }
}
