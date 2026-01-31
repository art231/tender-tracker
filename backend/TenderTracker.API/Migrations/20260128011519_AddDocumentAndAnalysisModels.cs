using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TenderTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentAndAnalysisModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenderDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenderId = table.Column<int>(type: "integer", nullable: false),
                    DocType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DownloadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceJson = table.Column<string>(type: "jsonb", nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderDocuments_FoundTenders_TenderId",
                        column: x => x.TenderId,
                        principalTable: "FoundTenders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechnologyAnalyses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenderId = table.Column<int>(type: "integer", nullable: false),
                    MatchScore = table.Column<int>(type: "integer", nullable: false),
                    MatchedTechnologiesJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsCompatible = table.Column<bool>(type: "boolean", nullable: false),
                    AnalyzedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AnalysisNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ManuallyVerified = table.Column<bool>(type: "boolean", nullable: false),
                    DocumentId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnologyAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnologyAnalyses_FoundTenders_TenderId",
                        column: x => x.TenderId,
                        principalTable: "FoundTenders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TechnologyAnalyses_TenderDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "TenderDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyAnalyses_AnalyzedAt",
                table: "TechnologyAnalyses",
                column: "AnalyzedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyAnalyses_DocumentId",
                table: "TechnologyAnalyses",
                column: "DocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyAnalyses_IsCompatible",
                table: "TechnologyAnalyses",
                column: "IsCompatible");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyAnalyses_ManuallyVerified",
                table: "TechnologyAnalyses",
                column: "ManuallyVerified");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyAnalyses_MatchScore",
                table: "TechnologyAnalyses",
                column: "MatchScore");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyAnalyses_TenderId",
                table: "TechnologyAnalyses",
                column: "TenderId");

            migrationBuilder.CreateIndex(
                name: "IX_TenderDocuments_DocType",
                table: "TenderDocuments",
                column: "DocType");

            migrationBuilder.CreateIndex(
                name: "IX_TenderDocuments_DownloadedAt",
                table: "TenderDocuments",
                column: "DownloadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenderDocuments_PublishedAt",
                table: "TenderDocuments",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenderDocuments_TenderId",
                table: "TenderDocuments",
                column: "TenderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TechnologyAnalyses");

            migrationBuilder.DropTable(
                name: "TenderDocuments");
        }
    }
}
