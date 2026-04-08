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
        Dictionary<int, string>? storedCategoryNames = null;

        if (File.Exists(_dataFilePath))
        {
            try
            {
                var json = File.ReadAllText(_dataFilePath);
                storedCategoryNames = LoadStoredCategoryNames(json);
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

        var requiresDataRewrite = NormalizeBudgetExpenses(storedCategoryNames);

        _nextBudgetId = _budgets.Count == 0
            ? 1
            : _budgets.Max(budget => budget.Id) + 1;

        _nextExpenseId = _budgets
            .SelectMany(budget => budget.Expenses)
            .DefaultIfEmpty()
            .Max(expense => expense?.Id ?? 0) + 1;

        if (requiresDataRewrite)
        {
            SaveData();
        }
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

            var category = ResolveUserCategoryOrNone(expense.CategoryId);

            var newExpense = new Expense
            {
                Id = _nextExpenseId++,
                Name = expense.Name,
                Amount = expense.Amount,
                CreatedAt = DateTime.Now,
                CategoryId = category?.Id ?? CategoryDefaults.NoCategoryId,
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
        return _db.Categories
            .OrderByDescending(category => category.IsSystem)
            .ThenBy(category => category.Name)
            .ToList();
    }

    public Category? AddCategory(string name)
    {
        var normalized = name.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        normalized = char.ToUpper(normalized[0]) + normalized.Substring(1);

        if (CategoryDefaults.IsNoCategoryName(normalized))
        {
            return null;
        }

        if (_db.Categories.Any(c => c.Name.ToLower() == normalized.ToLower()))
        {
            return null;
        }

        var newCategory = new Category { Name = normalized };

        _db.Categories.Add(newCategory);
        _db.SaveChanges();
        return newCategory;
    }

    public Category? UpdateCategory(int id, string name)
    {
        lock (_lock)
        {
            var category = _db.Categories.FirstOrDefault(c => c.Id == id);
            if (category == null || category.IsSystem || category.Id == CategoryDefaults.NoCategoryId)
            {
                return null;
            }

            var normalized = name.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            normalized = char.ToUpper(normalized[0]) + normalized.Substring(1);

            if (CategoryDefaults.IsNoCategoryName(normalized))
            {
                return null;
            }

            if (_db.Categories.Any(c => c.Id != id && c.Name.ToLower() == normalized.ToLower()))
            {
                return null;
            }

            category.Name = normalized;
            _db.SaveChanges();

            foreach (var expense in _budgets.SelectMany(budget => budget.Expenses).Where(expense => expense.CategoryId == id))
            {
                expense.Category = category;
            }

            SaveData();
            return category;
        }
    }

    public int GetCategoryUsageCount(int id)
    {
        lock (_lock)
        {
            return _budgets
                .SelectMany(budget => budget.Expenses)
                .Count(expense => expense.CategoryId == id);
        }
    }

    public bool DeleteCategory(int id)
    {
        lock (_lock)
        {
            if (id == CategoryDefaults.NoCategoryId)
                return false;

            var category = _db.Categories.FirstOrDefault(c => c.Id == id);
            if (category == null || category.IsSystem) return false;

            foreach (var expense in _budgets.SelectMany(budget => budget.Expenses).Where(expense => expense.CategoryId == id))
            {
                expense.CategoryId = CategoryDefaults.NoCategoryId;
                expense.Category = null;
            }

            _db.Categories.Remove(category);
            _db.SaveChanges();
            SaveData();
            return true;
        }
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

            var category = ResolveUserCategoryOrNone(request.CategoryId);

            expense.Name = request.Name;
            expense.Amount = request.Amount;
            expense.CreatedAt = request.CreatedAt;
            expense.CategoryId = category?.Id ?? CategoryDefaults.NoCategoryId;
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
                    CreatedAt = e.CreatedAt,
                    CategoryId = NormalizeCategoryId(e.CategoryId),
                    Category = ResolveCategoryReference(e.CategoryId, null),
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
            return PrepareBudgetForResponse(budget, categoryId: categoryId);
        }
    }

    public BudgetModel? GetMonthlyBudget(string month, int? categoryId = null)
    {
        lock (_lock)
        {
            var budget = GetOrCreateBudgetForMonth(month);
            return PrepareBudgetForResponse(budget, categoryId: categoryId);
        }
    }

    public BudgetModel? GetByMonth(
        string yearMonth,
        string? search = null,
        int? categoryId = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        lock (_lock)
        {
            Console.WriteLine($"Searching for month: {yearMonth}");

            foreach (var b in _budgets)
            {
                Console.WriteLine($"Existing: {b.Month}");
            }

            var budget = GetOrCreateBudgetForMonth(yearMonth);
            return PrepareBudgetForResponse(
                budget,
                search,
                categoryId,
                minAmount,
                maxAmount,
                fromDate,
                toDate);
        }
    }

    private BudgetModel? PrepareBudgetForResponse(
        BudgetModel? budget,
        string? search = null,
        int? categoryId = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
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
                    CreatedAt = expense.CreatedAt,
                    CategoryId = NormalizeCategoryId(expense.CategoryId),
                    Category = ResolveCategoryReference(expense.CategoryId, null),
                    MonthlyBudgetId = expense.MonthlyBudgetId
                })
                .ToList()
        };

        responseBudget.Expenses = ApplyExpenseFilters(
            responseBudget.Expenses,
            search,
            categoryId,
            minAmount,
            maxAmount,
            fromDate,
            toDate)
            .ToList();

        return responseBudget;
    }

    private BudgetModel GetOrCreateBudgetForMonth(string yearMonth)
    {
        var budget = _budgets.FirstOrDefault(b => b.Month == yearMonth);

        if (budget is not null)
        {
            budget.Expenses ??= [];
            return budget;
        }

        budget = new BudgetModel
        {
            Id = _nextBudgetId++,
            Month = yearMonth,
            Income = 0,
            Expenses = []
        };

        _budgets.Add(budget);
        SaveData();
        return budget;
    }

    private static IEnumerable<Expense> ApplyExpenseFilters(
        IEnumerable<Expense> expenses,
        string? search,
        int? categoryId,
        decimal? minAmount,
        decimal? maxAmount,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var filteredExpenses = expenses;

        if (!string.IsNullOrWhiteSpace(search))
        {
            filteredExpenses = filteredExpenses.Where(expense =>
                expense.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase));
        }

        if (categoryId.HasValue)
        {
            filteredExpenses = filteredExpenses.Where(expense => expense.CategoryId == categoryId.Value);
        }

        if (minAmount.HasValue)
        {
            filteredExpenses = filteredExpenses.Where(expense => expense.Amount >= minAmount.Value);
        }

        if (maxAmount.HasValue)
        {
            filteredExpenses = filteredExpenses.Where(expense => expense.Amount <= maxAmount.Value);
        }

        if (fromDate.HasValue)
        {
            filteredExpenses = filteredExpenses.Where(expense => expense.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            filteredExpenses = filteredExpenses.Where(expense => expense.CreatedAt <= toDate.Value);
        }

        return filteredExpenses;
    }

    private bool NormalizeBudgetExpenses(Dictionary<int, string>? storedCategoryNames)
    {
        var requiresDataRewrite = false;

        foreach (var budget in _budgets)
        {
            budget.Expenses ??= [];

            foreach (var expense in budget.Expenses)
            {
                if (expense.CreatedAt == default)
                {
                    expense.CreatedAt = DateTime.Now;
                    requiresDataRewrite = true;
                }

                expense.MonthlyBudgetId = budget.Id;
                expense.MonthlyBudget = budget;

                var storedCategoryName = storedCategoryNames is not null && storedCategoryNames.TryGetValue(expense.Id, out var name)
                    ? name
                    : null;

                var resolvedCategory = ResolveCategoryReference(expense.CategoryId, storedCategoryName);
                var normalizedCategoryId = resolvedCategory?.Id ?? CategoryDefaults.NoCategoryId;

                if (expense.CategoryId != normalizedCategoryId)
                {
                    expense.CategoryId = normalizedCategoryId;
                    requiresDataRewrite = true;
                }

                expense.Category = resolvedCategory;
            }
        }

        return requiresDataRewrite;
    }

    private Category? ResolveUserCategoryOrNone(int? categoryId)
    {
        var category = ResolveCategoryReference(categoryId, null);
        return category?.IsSystem == true
            ? null
            : category;
    }

    private Category? ResolveCategoryReference(int? categoryId, string? storedCategoryName)
    {
        if (!categoryId.HasValue || categoryId.Value == CategoryDefaults.NoCategoryId)
        {
            return null;
        }

        var category = _db.Categories.FirstOrDefault(c => c.Id == categoryId.Value);
        if (category is null || category.IsSystem)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(storedCategoryName)
            && !string.Equals(storedCategoryName, category.Name, StringComparison.CurrentCultureIgnoreCase))
        {
            return null;
        }

        return category;
    }

    private static int NormalizeCategoryId(int? categoryId)
    {
        return !categoryId.HasValue || categoryId.Value == CategoryDefaults.NoCategoryId
            ? CategoryDefaults.NoCategoryId
            : categoryId.Value;
    }

    private static Dictionary<int, string> LoadStoredCategoryNames(string json)
    {
        var result = new Dictionary<int, string>();

        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var budgetElement in document.RootElement.EnumerateArray())
        {
            if (!budgetElement.TryGetProperty("Expenses", out var expensesElement)
                || expensesElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var expenseElement in expensesElement.EnumerateArray())
            {
                if (!expenseElement.TryGetProperty("Id", out var idElement)
                    || idElement.ValueKind != JsonValueKind.Number
                    || !idElement.TryGetInt32(out var expenseId))
                {
                    continue;
                }

                if (expenseElement.TryGetProperty("CategoryName", out var categoryNameElement)
                    && categoryNameElement.ValueKind == JsonValueKind.String)
                {
                    result[expenseId] = categoryNameElement.GetString() ?? string.Empty;
                }
            }
        }

        return result;
    }

    private void SaveData()
    {
        var json = JsonSerializer.Serialize(_budgets, _jsonOptions);
        File.WriteAllText(_dataFilePath, json);
    }
}
