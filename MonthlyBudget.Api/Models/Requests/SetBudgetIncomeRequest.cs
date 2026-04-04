using System.ComponentModel.DataAnnotations;

namespace MonthlyBudget.Api.Models.Requests;

public class SetBudgetIncomeRequest
{
    [Range(typeof(decimal), "0", "999999999")]
    public decimal Income { get; set; }
}
