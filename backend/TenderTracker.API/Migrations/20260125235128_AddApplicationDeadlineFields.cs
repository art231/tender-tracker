using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenderTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationDeadlineFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdditionalInfo",
                table: "FoundTenders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApplicationDeadline",
                table: "FoundTenders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerInn",
                table: "FoundTenders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxPrice",
                table: "FoundTenders",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "FoundTenders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalInfo",
                table: "FoundTenders");

            migrationBuilder.DropColumn(
                name: "ApplicationDeadline",
                table: "FoundTenders");

            migrationBuilder.DropColumn(
                name: "CustomerInn",
                table: "FoundTenders");

            migrationBuilder.DropColumn(
                name: "MaxPrice",
                table: "FoundTenders");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "FoundTenders");
        }
    }
}
