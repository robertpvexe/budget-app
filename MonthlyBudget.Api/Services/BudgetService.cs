using System.Text.Json;
using MonthlyBudget.Api.Models;
using BudgetModel = MonthlyBudget.Api.Models.MonthlyBudget;

namespace MonthlyBudget.Api.Services;

public class BudgetService : IBudgetService
{
    private List<BudgetModel> _budgets;
    private readonly Lock _lock = new();
    private readonly string _dataFilePath = Path.Combine(AppContext.BaseDirectory, "data.json");
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };
    private int _nextBudgetId = 1;
    private int _nextExpenseId = 1;

    public BudgetService()
    {
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
            var budget = _budgets.FirstOrDefault(b => b.Id == monthlyBudgetId);

            if (budget is null)
            {
                throw new InvalidOperationException($"Monthly budget with id '{monthlyBudgetId}' does not exist.");
            }

            var newExpense = new Expense
            {
                Id = _nextExpenseId++,
                Name = expense.Name,
                Amount = expense.Amount,
                Category = expense.Category,
                MonthlyBudgetId = budget.Id,
                MonthlyBudget = budget
            };

            budget.Expenses.Add(newExpense);
            SaveData();
            return newExpense;
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

    public BudgetModel? GetMonthlyBudget(int id)
    {
        lock (_lock)
        {
            return _budgets.FirstOrDefault(b => b.Id == id);
        }
    }

    public BudgetModel? GetMonthlyBudget(string month)
    {
        lock (_lock)
        {
            return _budgets.FirstOrDefault(b => b.Month == month);
        }
    }

    public BudgetModel? GetByMonth(string yearMonth)
    {
        lock (_lock)
        {
            Console.WriteLine($"Searching for month: {yearMonth}");

            foreach (var b in _budgets)
            {
                Console.WriteLine($"Existing: {b.Month}");
            }

            return _budgets.FirstOrDefault(b => b.Month == yearMonth);
        }
    }

    private void SaveData()
    {
        var json = JsonSerializer.Serialize(_budgets, _jsonOptions);
        File.WriteAllText(_dataFilePath, json);
    }
}
