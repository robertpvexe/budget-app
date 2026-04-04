using Microsoft.AspNetCore.Mvc;
using MonthlyBudget.Api.Models;
using MonthlyBudget.Api.Models.Requests;
using MonthlyBudget.Api.Models.Responses;
using MonthlyBudget.Api.Services;
using System.Globalization;
using BudgetModel = MonthlyBudget.Api.Models.MonthlyBudget;

namespace MonthlyBudget.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BudgetController : ControllerBase
{
    private readonly IBudgetService _budgetService;

    public BudgetController(IBudgetService budgetService)
    {
        _budgetService = budgetService;
    }

    [HttpGet("test")]
    public IActionResult Test()
    {
        Console.WriteLine("=== TEST ENDPOINT HIT ===");
        return Ok("API działa");
    }

    [HttpPost]
    public ActionResult<BudgetModel> CreateMonthlyBudget(CreateMonthlyBudgetRequest request)
    {
        if (!IsValidMonth(request.Month))
        {
            return BadRequest("Month must have format yyyy-MM.");
        }

        var budget = new BudgetModel
        {
            Month = request.Month,
            Income = request.Income
        };

        var createdBudget = _budgetService.CreateMonthlyBudget(budget);
        return CreatedAtAction(nameof(GetMonthlyBudget), new { id = createdBudget.Id }, createdBudget);
    }

    [HttpPost("{id:int}/expense")]
    public ActionResult<Expense> AddExpense(int id, AddExpenseRequest request)
    {
        var expense = new Expense
        {
            Name = request.Name,
            Amount = request.Amount,
            Category = request.Category
        };

        try
        {
            var createdExpense = _budgetService.AddExpense(id, expense);
            return CreatedAtAction(nameof(GetMonthlyBudget), new { id }, createdExpense);
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(exception.Message);
        }
    }

    [HttpDelete("{budgetId:int}/expense/{expenseId:int}")]
    public IActionResult DeleteExpense(int budgetId, int expenseId)
    {
        Console.WriteLine("=== CONTROLLER DELETE HIT ===");
        Console.WriteLine($"budgetId: {budgetId}, expenseId: {expenseId}");

        var result = _budgetService.RemoveExpense(budgetId, expenseId);

        if (!result)
            return NotFound();

        return Ok();
    }

    [HttpPut("{id}")]
    public IActionResult UpdateBudget(int id, [FromBody] UpdateIncomeRequest request)
    {
        Console.WriteLine($"=== UPDATE BUDGET === id: {id}, income: {request.Income}");

        var result = _budgetService.UpdateIncome(id, request.Income);

        if (!result)
            return NotFound("Budget not found");

        return Ok();
    }

    [HttpGet("{id:int}")]
    public ActionResult<BudgetDetailsResponse> GetMonthlyBudget(int id)
    {
        var budget = _budgetService.GetMonthlyBudget(id);

        if (budget is null)
        {
            return NotFound();
        }

        return Ok(new BudgetDetailsResponse
        {
            Id = budget.Id,
            Month = budget.Month,
            Income = budget.Income,
            Expenses = budget.Expenses,
            TotalExpenses = budget.GetTotalExpenses(),
            Remaining = budget.GetRemainingAmount(),
            Percentage = budget.GetExpensePercentage()
        });
    }

    [HttpGet("month/{month}")]
    public ActionResult<BudgetDetailsResponse> GetMonthlyBudgetByMonth(string month)
    {
        if (!IsValidMonth(month))
        {
            return BadRequest("Month must have format yyyy-MM.");
        }

        var budget = _budgetService.GetMonthlyBudget(month);

        if (budget is null)
        {
            return NotFound();
        }

        return Ok(new BudgetDetailsResponse
        {
            Id = budget.Id,
            Month = budget.Month,
            Income = budget.Income,
            Expenses = budget.Expenses,
            TotalExpenses = budget.GetTotalExpenses(),
            Remaining = budget.GetRemainingAmount(),
            Percentage = budget.GetExpensePercentage()
        });
    }

    [HttpGet("by-month/{yearMonth}")]
    public IActionResult GetByMonth(string yearMonth)
    {
        Console.WriteLine($"=== GET BY MONTH === {yearMonth}");

        var budget = _budgetService.GetByMonth(yearMonth);

        if (budget is null)
            return NotFound("Budget not found");

        return Ok(budget);
    }

    private static bool IsValidMonth(string month) =>
        DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
