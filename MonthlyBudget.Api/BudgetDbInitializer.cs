using Microsoft.EntityFrameworkCore;

namespace MonthlyBudget.Api;

public static class BudgetDbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();

        await dbContext.Database.MigrateAsync();
        await SeedCategoriesAsync(dbContext);
    }

    private static async Task SeedCategoriesAsync(BudgetDbContext dbContext)
    {
        var existingCategories = await dbContext.Categories
            .OrderBy(category => category.Id)
            .ToListAsync();

        var isCategoryTableEmpty = existingCategories.Count == 0;
        var hasChanges = false;
        var existingNames = new HashSet<string>(
            existingCategories.Select(category => category.Name),
            StringComparer.CurrentCultureIgnoreCase);

        var noCategory = existingCategories.FirstOrDefault(category =>
            category.IsSystem || CategoryDefaults.IsNoCategoryName(category.Name));

        if (noCategory is null)
        {
            dbContext.Categories.Add(new Category
            {
                Name = CategoryDefaults.NoCategoryName,
                IsSystem = true
            });

            existingNames.Add(CategoryDefaults.NoCategoryName);
            hasChanges = true;
        }
        else if (!noCategory.IsSystem)
        {
            noCategory.IsSystem = true;
            hasChanges = true;
        }

        if (isCategoryTableEmpty)
        {
            foreach (var categoryName in CategoryDefaults.StarterCategoryNames)
            {
                if (existingNames.Contains(categoryName))
                {
                    continue;
                }

                dbContext.Categories.Add(new Category
                {
                    Name = categoryName,
                    IsSystem = CategoryDefaults.IsNoCategoryName(categoryName)
                });

                existingNames.Add(categoryName);
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await dbContext.SaveChangesAsync();
        }
    }
}
