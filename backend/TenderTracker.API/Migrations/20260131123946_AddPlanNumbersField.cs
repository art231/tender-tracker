using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenderTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanNumbersField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlanNumbersJson",
                table: "FoundTenders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlanNumbersJson",
                table: "FoundTenders");
        }
    }
}
