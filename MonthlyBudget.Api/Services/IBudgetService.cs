using BudgetModel = MonthlyBudget.Api.MonthlyBudget;

namespace MonthlyBudget.Api;

public interface IBudgetService
{
    Expense AddExpense(int monthlyBudgetId, Expense expense);
    List<Category> GetCategories();
    Category? AddCategory(string name);
    Category? UpdateCategory(int id, string name);
    int GetCategoryUsageCount(int id);
    bool DeleteCategory(int id);
    bool UpdateExpense(int budgetId, int expenseId, UpdateExpenseRequest request);
    bool RemoveExpense(int monthlyBudgetId, int expenseId);
    BudgetModel? FilterBudget(int year, int month, int? categoryId, DateTime? from, DateTime? to);
    BudgetModel? GetMonthlyBudget(int id, int? categoryId = null);
    BudgetModel? GetMonthlyBudget(string month, int? categoryId = null);
    BudgetModel? GetByMonth(
        string yearMonth,
        string? search = null,
        int? categoryId = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);
}
