using System.Text.Json;
using BudgetModel = MonthlyBudget.Api.MonthlyBudget;

namespace MonthlyBudget.Api;

public class BudgetService : IBudgetService
{
    private readonly BudgetDbContext _db;
    private List<BudgetModel> _budgets;
    private readonly Lock _lock = new();
    private readonly string _dataFilePath = Path.Combine(AppContext.BaseDirectory, "data.json");
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };
    private int _nextBudgetId = 1;
    private int _nextExpenseId = 1;

    public BudgetService(BudgetDbContext db)
    {
        _db = db;

        if (File.Exists(_dataFilePath))
        {
            try
            {
                var json = File.ReadAllText(_dataFilePath);
                _budgets = JsonSerializer.Deserialize<List<BudgetModel>>(json, _jsonOptions) ?? new List<BudgetModel>();
            }
            catch
            {
                _budgets = new List<BudgetModel>();
            }
        }
        else
        {
            _budgets = new List<BudgetModel>();
        }

        foreach (var budget in _budgets)
        {
            budget.Expenses ??= [];

            foreach (var expense in budget.Expenses)
            {
                expense.MonthlyBudgetId = budget.Id;
                expense.MonthlyBudget = budget;
                expense.Category = _db.Categories.FirstOrDefault(c => c.Id == expense.CategoryId);
            }
        }

        _nextBudgetId = _budgets.Count == 0
            ? 1
            : _budgets.Max(budget => budget.Id) + 1;

        _nextExpenseId = _budgets
            .SelectMany(budget => budget.Expenses)
            .DefaultIfEmpty()
            .Max(expense => expense?.Id ?? 0) + 1;
    }

    public BudgetModel CreateMonthlyBudget(BudgetModel monthlyBudget)
    {
        lock (_lock)
        {
            var existingBudget = _budgets.FirstOrDefault(b => b.Month == monthlyBudget.Month);

            if (existingBudget is not null)
            {
                return existingBudget;
            }

            var budget = new BudgetModel
            {
                Id = _nextBudgetId++,
                Month = monthlyBudget.Month,
                Income = monthlyBudget.Income,
                Expenses = []
            };

            _budgets.Add(budget);
            SaveData();
            return budget;
        }
    }

    public Expense AddExpense(int monthlyBudgetId, Expense expense)
    {
        lock (_lock)
        {
            if (!Expense.IsAmountValid(expense.Amount))
            {
                throw new ArgumentOutOfRangeException(nameof(expense.Amount), $"Amount must be between {Expense.MinAmount} and {Expense.MaxAmount}.");
            }

            var budget = _budgets.FirstOrDefault(b => b.Id == monthlyBudgetId);

            if (budget is null)
            {
                throw new InvalidOperationException($"Monthly budget with id '{monthlyBudgetId}' does not exist.");
            }

            var category = _db.Categories.FirstOrDefault(c => c.Id == expense.CategoryId)
                ?? _db.Categories.FirstOrDefault(c => c.Id == 0);

            var newExpense = new Expense
            {
                Id = _nextExpenseId++,
                Name = expense.Name,
                Amount = expense.Amount,
                CategoryId = category?.Id ?? 0,
                Category = category,
                MonthlyBudgetId = budget.Id,
                MonthlyBudget = budget
            };

            budget.Expenses.Add(newExpense);
            SaveData();
            return newExpense;
        }
    }

    public List<Category> GetCategories()
    {
        return _db.Categories.ToList();
    }

    public Category? AddCategory(string name)
    {
        var normalized = name.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        normalized = char.ToUpper(normalized[0]) + normalized.Substring(1);

        if (normalized == "Brak kategorii")
        {
            return null;
        }

        if (_db.Categories.Any(c => c.Name.ToLower() == normalized.ToLower()))
        {
            return null;
        }

        var newCategory = new Category
        {
            Id = _db.Categories.Any() ? _db.Categories.Max(c => c.Id) + 1 : 1,
            Name = normalized
        };

        _db.Categories.Add(newCategory);
        _db.SaveChanges();
        return newCategory;
    }

    public bool DeleteCategory(int id)
    {
        if (id == 0)
            return false;

        var category = _db.Categories.FirstOrDefault(c => c.Id == id);
        if (category == null) return false;

        _db.Categories.Remove(category);
        _db.SaveChanges();
        return true;
    }

    public bool RemoveExpense(int monthlyBudgetId, int expenseId)
    {
        lock (_lock)
        {
            Console.WriteLine("=== REMOVE DEBUG ===");
            Console.WriteLine($"budgetId: {monthlyBudgetId}, expenseId: {expenseId}");

            var budget = _budgets.FirstOrDefault(b => b.Id == monthlyBudgetId);

            if (budget is null)
            {
                return false;
            }

            Console.WriteLine($"Budget found: {budget.Id}");
            budget.Expenses ??= [];
            Console.WriteLine("=== DEBUG REMOVE ===");
            Console.WriteLine($"budgetId: {monthlyBudgetId}");
            Console.WriteLine($"expenseId: {expenseId}");
            foreach (var e in budget.Expenses)
            {
                Console.WriteLine($"Existing expense: Id={e.Id}, Name={e.Name}");
            }
            Console.WriteLine($"Budgets count: {_budgets.Count}");
            Console.WriteLine($"Expenses count: {budget.Expenses.Count}");
            var expense = budget.Expenses.FirstOrDefault(e => e.Id == expenseId);

            if (expense is null)
            {
                Console.WriteLine($"Expense '{expenseId}' not found. Available expense ids: {string.Join(", ", budget.Expenses.Select(e => e.Id))}");
                return false;
            }

            budget.Expenses.Remove(expense);
            SaveData();
            return true;
        }
    }

    public bool UpdateExpense(int budgetId, int expenseId, UpdateExpenseRequest request)
    {
        lock (_lock)
        {
            if (!Expense.IsAmountValid(request.Amount))
            {
                return false;
            }

            var budget = _budgets.FirstOrDefault(b => b.Id == budgetId);
            if (budget == null) return false;

            var expense = budget.Expenses.FirstOrDefault(e => e.Id == expenseId);
            if (expense == null) return false;

            var category = _db.Categories.FirstOrDefault(c => c.Id == request.CategoryId)
                ?? _db.Categories.FirstOrDefault(c => c.Id == 0);

            expense.Name = request.Name;
            expense.Amount = request.Amount;
            expense.CategoryId = category?.Id ?? 0;
            expense.Category = category;

            SaveData();
            return true;
        }
    }

    public bool UpdateIncome(int id, decimal income)
    {
        lock (_lock)
        {
            var budget = _budgets.FirstOrDefault(b => b.Id == id);

            if (budget == null)
                return false;

            budget.Income = income;

            SaveData();

            return true;
        }
    }

    public BudgetModel? FilterBudget(int year, int month, int? categoryId, DateTime? from, DateTime? to)
    {
        var key = $"{year}-{month:D2}";
        var budget = _budgets.FirstOrDefault(b => b.Month == key);

        if (budget is null)
        {
            return null;
        }

        var filteredBudget = new BudgetModel
        {
            Id = budget.Id,
            Month = budget.Month,
            Income = budget.Income,
            Expenses = budget.Expenses
                .Select(e => new Expense
                {
                    Id = e.Id,
                    Name = e.Name,
                    Amount = e.Amount,
                    CategoryId = e.CategoryId,
                    Category = e.Category ?? _db.Categories.FirstOrDefault(c => c.Id == e.CategoryId),
                    MonthlyBudgetId = e.MonthlyBudgetId
                })
                .ToList()
        };

        var expenses = filteredBudget.Expenses.AsQueryable();

        if (categoryId.HasValue && categoryId.Value != 0)
        {
            expenses = expenses.Where(e => e.CategoryId == categoryId.Value);
        }

        // Expense currently has no Date field, so from/to are intentionally ignored for now.
        filteredBudget.Expenses = expenses.ToList();
        return filteredBudget;
    }

    public BudgetModel? GetMonthlyBudget(int id, int? categoryId = null)
    {
        lock (_lock)
        {
            var budget = _budgets.FirstOrDefault(b => b.Id == id);
            return PrepareBudgetForResponse(budget, categoryId);
        }
    }

    public BudgetModel? GetMonthlyBudget(string month, int? categoryId = null)
    {
        lock (_lock)
        {
            var budget = _budgets.FirstOrDefault(b => b.Month == month);
            return PrepareBudgetForResponse(budget, categoryId);
        }
    }

    public BudgetModel? GetByMonth(string yearMonth, int? categoryId = null)
    {
        lock (_lock)
        {
            Console.WriteLine($"Searching for month: {yearMonth}");

            foreach (var b in _budgets)
            {
                Console.WriteLine($"Existing: {b.Month}");
            }

            var budget = _budgets.FirstOrDefault(b => b.Month == yearMonth);
            return PrepareBudgetForResponse(budget, categoryId);
        }
    }

    private BudgetModel? PrepareBudgetForResponse(BudgetModel? budget, int? categoryId)
    {
        if (budget is null)
        {
            return null;
        }

        var responseBudget = new BudgetModel
        {
            Id = budget.Id,
            Month = budget.Month,
            Income = budget.Income,
            Expenses = budget.Expenses
                .Select(expense => new Expense
                {
                    Id = expense.Id,
                    Name = expense.Name,
                    Amount = expense.Amount,
                    CategoryId = expense.CategoryId,
                    Category = expense.Category ?? _db.Categories.FirstOrDefault(c => c.Id == expense.CategoryId),
                    MonthlyBudgetId = expense.MonthlyBudgetId
                })
                .ToList()
        };

        if (categoryId.HasValue)
        {
            responseBudget.Expenses = responseBudget.Expenses
                .Where(e => e.CategoryId == categoryId.Value)
                .ToList();
        }
        return responseBudget;
    }

    private void SaveData()
    {
        var json = JsonSerializer.Serialize(_budgets, _jsonOptions);
        File.WriteAllText(_dataFilePath, json);
    }
}
