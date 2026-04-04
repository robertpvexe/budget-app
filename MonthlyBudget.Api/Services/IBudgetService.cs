using MonthlyBudget.Api.Models;
using BudgetModel = MonthlyBudget.Api.Models.MonthlyBudget;

namespace MonthlyBudget.Api.Services;

public interface IBudgetService
{
    BudgetModel CreateMonthlyBudget(BudgetModel monthlyBudget);
    Expense AddExpense(int monthlyBudgetId, Expense expense);
    bool RemoveExpense(int monthlyBudgetId, int expenseId);
    bool UpdateIncome(int id, decimal income);
    BudgetModel? GetMonthlyBudget(int id);
    BudgetModel? GetMonthlyBudget(string month);
    BudgetModel? GetByMonth(string yearMonth);
}
