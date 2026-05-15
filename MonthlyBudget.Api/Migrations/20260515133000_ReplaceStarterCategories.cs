using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonthlyBudget.Api.Migrations
{
    [Migration("20260515133000_ReplaceStarterCategories")]
    public partial class ReplaceStarterCategories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO Categories (Name, IsSystem)
                SELECT 'Brak kategorii', 1
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM Categories
                    WHERE lower(trim(Name)) = 'brak kategorii'
                );
                """);

            migrationBuilder.Sql("""
                UPDATE Categories
                SET IsSystem = 1
                WHERE lower(trim(Name)) = 'brak kategorii';
                """);

            migrationBuilder.Sql("""
                UPDATE Expenses
                SET CategoryId = NULL
                WHERE CategoryId IN (
                    SELECT Id
                    FROM Categories
                    WHERE IsSystem = 0
                      AND lower(trim(Name)) <> 'brak kategorii'
                );
                """);

            migrationBuilder.Sql("""
                DELETE FROM Categories
                WHERE IsSystem = 0
                  AND lower(trim(Name)) <> 'brak kategorii';
                """);

            InsertCategory(migrationBuilder, "DOM");
            InsertCategory(migrationBuilder, "JEDZENIE");
            InsertCategory(migrationBuilder, "TRANSPORT");
            InsertCategory(migrationBuilder, "SAMOCHÓD");
            InsertCategory(migrationBuilder, "RACHUNKI");
            InsertCategory(migrationBuilder, "ZDROWIE");
            InsertCategory(migrationBuilder, "PRACA");
            InsertCategory(migrationBuilder, "ROZRYWKA");
            InsertCategory(migrationBuilder, "ZAKUPY");
            InsertCategory(migrationBuilder, "SUBSKRYPCJE");
            InsertCategory(migrationBuilder, "ZWIERZĘTA");
            InsertCategory(migrationBuilder, "PODRÓŻE");
            InsertCategory(migrationBuilder, "PREZENTY");
            InsertCategory(migrationBuilder, "EDUKACJA");
            InsertCategory(migrationBuilder, "OSZCZĘDNOŚCI");
            InsertCategory(migrationBuilder, "INNE");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Expenses
                SET CategoryId = NULL
                WHERE CategoryId IN (
                    SELECT Id
                    FROM Categories
                    WHERE IsSystem = 0
                      AND lower(trim(Name)) <> 'brak kategorii'
                );
                """);

            migrationBuilder.Sql("""
                DELETE FROM Categories
                WHERE IsSystem = 0
                  AND lower(trim(Name)) <> 'brak kategorii';
                """);

            InsertCategory(migrationBuilder, "Jedzenie");
            InsertCategory(migrationBuilder, "Napoje");
            InsertCategory(migrationBuilder, "Rachunki");
            InsertCategory(migrationBuilder, "Subskrypcje");
            InsertCategory(migrationBuilder, "Inne");
        }

        private static void InsertCategory(MigrationBuilder migrationBuilder, string name)
        {
            migrationBuilder.Sql($"""
                INSERT INTO Categories (Name, IsSystem)
                SELECT '{name}', 0
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM Categories
                    WHERE lower(trim(Name)) = lower('{name}')
                );
                """);
        }
    }
}
