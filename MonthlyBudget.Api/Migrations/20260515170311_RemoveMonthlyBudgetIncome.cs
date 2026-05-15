using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonthlyBudget.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMonthlyBudgetIncome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Income",
                table: "MonthlyBudget");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Income",
                table: "MonthlyBudget",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
