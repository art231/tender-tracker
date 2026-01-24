using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TenderTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SearchQueries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Keyword = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchQueries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FoundTenders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PurchaseNumber = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PublishDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DirectLinkToSource = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FoundByQueryId = table.Column<int>(type: "integer", nullable: true),
                    SavedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoundTenders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoundTenders_SearchQueries_FoundByQueryId",
                        column: x => x.FoundByQueryId,
                        principalTable: "SearchQueries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoundTenders_ExternalId",
                table: "FoundTenders",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FoundTenders_FoundByQueryId",
                table: "FoundTenders",
                column: "FoundByQueryId");

            migrationBuilder.CreateIndex(
                name: "IX_FoundTenders_PublishDate",
                table: "FoundTenders",
                column: "PublishDate");

            migrationBuilder.CreateIndex(
                name: "IX_FoundTenders_SavedAt",
                table: "FoundTenders",
                column: "SavedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SearchQueries_CreatedAt",
                table: "SearchQueries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SearchQueries_IsActive",
                table: "SearchQueries",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FoundTenders");

            migrationBuilder.DropTable(
                name: "SearchQueries");
        }
    }
}
