using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonthlyBudget.Api.Migrations
{
    [Migration("20260515134500_SeedStandardCategories")]
    public partial class SeedStandardCategories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                DELETE FROM Categories
                WHERE IsSystem = 0
                  AND lower(trim(Name)) IN (
                      'dom',
                      'jedzenie',
                      'transport',
                      'samochód',
                      'rachunki',
                      'zdrowie',
                      'praca',
                      'rozrywka',
                      'zakupy',
                      'subskrypcje',
                      'zwierzęta',
                      'podróże',
                      'prezenty',
                      'edukacja',
                      'oszczędności',
                      'inne'
                  );
                """);
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
