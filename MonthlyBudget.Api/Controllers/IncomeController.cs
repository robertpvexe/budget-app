using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MonthlyBudget.Api;

[ApiController]
[Route("api/[controller]")]
public class IncomeController : ControllerBase
{
    private readonly BudgetDbContext _dbContext;

    public IncomeController(BudgetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<IncomeEntry>>> GetAll()
    {
        var entries = await _dbContext.IncomeEntries
            .AsNoTracking()
            .OrderByDescending(entry => entry.Date)
            .ThenByDescending(entry => entry.Id)
            .ToListAsync();

        return Ok(entries);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<IncomeSummaryResponse>> GetSummary([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        if (startDate == default || endDate == default)
        {
            return BadRequest("startDate and endDate are required.");
        }

        var normalizedStartDate = startDate.Date;
        var normalizedEndDate = endDate.Date;

        if (normalizedStartDate > normalizedEndDate)
        {
            return BadRequest("startDate must be earlier than or equal to endDate.");
        }

        var totalIncome = await _dbContext.IncomeEntries
            .AsNoTracking()
            .Where(entry => entry.Date >= normalizedStartDate && entry.Date < normalizedEndDate.AddDays(1))
            .SumAsync(entry => (decimal?)entry.Amount) ?? 0m;

        return Ok(new IncomeSummaryResponse
        {
            TotalIncome = totalIncome
        });
    }

    [HttpPost]
    public async Task<ActionResult<IncomeEntry>> Create(CreateIncomeEntryRequest request)
    {
        if (!IncomeEntry.IsAmountValid(request.Amount))
        {
            return BadRequest("Amount must be greater than 0.");
        }

        var description = (request.Description ?? string.Empty).Trim();
        if (description.Length > IncomeEntry.MaxDescriptionLength)
        {
            return BadRequest($"Description must not exceed {IncomeEntry.MaxDescriptionLength} characters.");
        }

        var effectiveDate = request.Date == default
            ? DateTime.Today
            : request.Date.Date;

        var entry = new IncomeEntry
        {
            Amount = request.Amount,
            Date = effectiveDate,
            Description = description
        };

        _dbContext.IncomeEntries.Add(entry);
        await _dbContext.SaveChangesAsync();

        return Created($"/api/income/{entry.Id}", entry);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<IncomeEntry>> Update(int id, CreateIncomeEntryRequest request)
    {
        if (!IncomeEntry.IsAmountValid(request.Amount))
        {
            return BadRequest("Amount must be greater than 0.");
        }

        var entry = await _dbContext.IncomeEntries.FindAsync(id);
        if (entry is null)
        {
            return NotFound();
        }

        var description = (request.Description ?? string.Empty).Trim();
        if (description.Length > IncomeEntry.MaxDescriptionLength)
        {
            return BadRequest($"Description must not exceed {IncomeEntry.MaxDescriptionLength} characters.");
        }

        var effectiveDate = request.Date == default
            ? DateTime.Today
            : request.Date.Date;

        entry.Amount = request.Amount;
        entry.Date = effectiveDate;
        entry.Description = description;

        await _dbContext.SaveChangesAsync();

        return Ok(entry);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entry = await _dbContext.IncomeEntries.FindAsync(id);
        if (entry is null)
        {
            return NotFound();
        }

        _dbContext.IncomeEntries.Remove(entry);
        await _dbContext.SaveChangesAsync();

        return Ok();
    }
}
