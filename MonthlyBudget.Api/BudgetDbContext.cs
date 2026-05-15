using Microsoft.EntityFrameworkCore;

namespace MonthlyBudget.Api;

public class BudgetDbContext : DbContext
{
    public BudgetDbContext(DbContextOptions<BudgetDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<IncomeEntry> IncomeEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Expense>()
            .HasOne(e => e.Category)
            .WithMany(c => c.Expenses)
            .HasForeignKey(e => e.CategoryId)
            .IsRequired(false);

        modelBuilder.Entity<IncomeEntry>()
            .Property(entry => entry.Amount)
            .HasColumnType("TEXT");

        modelBuilder.Entity<IncomeEntry>()
            .Property(entry => entry.Description)
            .HasMaxLength(IncomeEntry.MaxDescriptionLength);
    }
}
