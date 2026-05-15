using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;
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
            CreatedAt = request.CreatedAt,
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

    [HttpGet("/api/categories/{id:int}/usage")]
    public IActionResult GetCategoryUsage(int id)
    {
        return Ok(new { count = _budgetService.GetCategoryUsageCount(id) });
    }

    [HttpPost("categories")]
    public IActionResult AddCategory([FromBody] string name)
    {
        var category = _budgetService.AddCategory(name);
        if (category is null)
            return BadRequest("Category already exists or name is invalid.");

        return Ok(category);
    }

    [HttpPut("categories/{id:int}")]
    [HttpPut("/api/categories/{id:int}")]
    public IActionResult UpdateCategory(int id, [FromBody] JsonElement requestBody)
    {
        if (!_budgetService.GetCategories().Any(category => category.Id == id))
            return NotFound();

        var name = requestBody.ValueKind switch
        {
            JsonValueKind.String => requestBody.GetString() ?? string.Empty,
            JsonValueKind.Object when requestBody.TryGetProperty("name", out var nameProperty) => nameProperty.GetString() ?? string.Empty,
            JsonValueKind.Object when requestBody.TryGetProperty("Name", out var legacyNameProperty) => legacyNameProperty.GetString() ?? string.Empty,
            _ => string.Empty
        };

        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Category name is required.");

        var category = _budgetService.UpdateCategory(id, name);
        if (category is null)
            return BadRequest("Category is protected or name is invalid.");

        return Ok();
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
            Expenses = budget.Expenses,
            TotalExpenses = budget.GetTotalExpenses()
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
            Expenses = budget.Expenses,
            TotalExpenses = budget.GetTotalExpenses()
        });
    }

    [HttpGet("by-month/{yearMonth}")]
    public ActionResult<BudgetDetailsResponse> GetByMonth(
        string yearMonth,
        [FromQuery] string? search = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] decimal? minAmount = null,
        [FromQuery] decimal? maxAmount = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        Console.WriteLine($"=== GET BY MONTH === {yearMonth}");

        if (!IsValidMonth(yearMonth))
        {
            return BadRequest("Month must have format yyyy-MM.");
        }

        var budget = _budgetService.GetByMonth(
            yearMonth,
            search,
            categoryId,
            minAmount,
            maxAmount,
            fromDate,
            toDate);

        if (budget is null)
        {
            return NotFound("Budget not found");
        }

        return Ok(new BudgetDetailsResponse
        {
            Id = budget.Id,
            Month = budget.Month,
            Expenses = budget.Expenses,
            TotalExpenses = budget.GetTotalExpenses()
        });
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
