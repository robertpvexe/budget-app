using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonthlyBudget.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIsSystemToCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                table: "Categories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE Categories
                SET IsSystem = 1
                WHERE lower(trim(Name)) = 'brak kategorii';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSystem",
                table: "Categories");
        }
    }
}
