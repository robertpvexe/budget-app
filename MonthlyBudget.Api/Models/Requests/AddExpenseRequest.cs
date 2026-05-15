using System.ComponentModel.DataAnnotations;

namespace MonthlyBudget.Api;

public class AddExpenseRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public int CategoryId { get; set; }
}
