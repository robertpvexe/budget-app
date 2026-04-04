using System.ComponentModel.DataAnnotations;

namespace MonthlyBudget.Api.Models.Requests;

public class AddExpenseRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;
}
