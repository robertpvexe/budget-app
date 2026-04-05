using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using BudgetModel = MonthlyBudget.Api.MonthlyBudget;

namespace MonthlyBudget.Api;

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
        if (!Expense.IsAmountValid(request.Amount))
        {
            return BadRequest($"Amount must be between {Expense.MinAmount} and {Expense.MaxAmount}.");
        }

        var expense = new Expense
        {
            Name = request.Name,
            Amount = request.Amount,
            CategoryId = request.CategoryId
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

    [HttpGet("categories")]
    public IActionResult GetCategories()
    {
        return Ok(_budgetService.GetCategories());
    }

    [HttpPost("categories")]
    public IActionResult AddCategory([FromBody] string name)
    {
        var category = _budgetService.AddCategory(name);
        if (category is null)
            return BadRequest("Category already exists or name is invalid.");

        return Ok(category);
    }

    [HttpDelete("categories/{id}")]
    public IActionResult DeleteCategory(int id)
    {
        var result = _budgetService.DeleteCategory(id);
        if (!result) return NotFound();

        return Ok();
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

    [HttpPut("{budgetId:int}/expense/{expenseId:int}")]
    public IActionResult UpdateExpense(int budgetId, int expenseId, [FromBody] UpdateExpenseRequest request)
    {
        Console.WriteLine("=== UPDATE EXPENSE CONTROLLER HIT ===");

        if (!Expense.IsAmountValid(request.Amount))
        {
            return BadRequest($"Amount must be between {Expense.MinAmount} and {Expense.MaxAmount}.");
        }

        var result = _budgetService.UpdateExpense(budgetId, expenseId, request);

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
    public IActionResult GetByMonth(string yearMonth, [FromQuery] int? categoryId = null)
    {
        Console.WriteLine($"=== GET BY MONTH === {yearMonth}");

        var budget = _budgetService.GetByMonth(yearMonth, categoryId);

        if (budget is null)
            return NotFound("Budget not found");

        return Ok(budget);
    }

    [HttpGet("filter")]
    public IActionResult Filter(
        int year,
        int month,
        int? categoryId,
        DateTime? from,
        DateTime? to)
    {
        var result = _budgetService.FilterBudget(year, month, categoryId, from, to);
        return Ok(result);
    }

    private static bool IsValidMonth(string month) =>
        DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
