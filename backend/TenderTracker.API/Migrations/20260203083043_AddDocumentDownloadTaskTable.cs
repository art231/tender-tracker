using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TenderTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentDownloadTaskTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentDownloadTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenderId = table.Column<int>(type: "integer", nullable: false),
                    DocType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SettingsJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentDownloadTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentDownloadTasks_FoundTenders_TenderId",
                        column: x => x.TenderId,
                        principalTable: "FoundTenders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentDownloadTasks_CreatedAt",
                table: "DocumentDownloadTasks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentDownloadTasks_NextRetryAt",
                table: "DocumentDownloadTasks",
                column: "NextRetryAt");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentDownloadTasks_Priority",
                table: "DocumentDownloadTasks",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentDownloadTasks_Status",
                table: "DocumentDownloadTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentDownloadTasks_TenderId",
                table: "DocumentDownloadTasks",
                column: "TenderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentDownloadTasks");
        }
    }
}
