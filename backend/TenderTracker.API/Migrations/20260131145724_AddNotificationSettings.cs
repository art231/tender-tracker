using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TenderTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    NotificationType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EmailAddress = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    TelegramChatId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    WebhookUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NotifyOnNewTenders = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnDeadlineApproaching = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnTechnologyMatch = table.Column<bool>(type: "boolean", nullable: false),
                    DeadlineWarningDays = table.Column<int>(type: "integer", nullable: false),
                    FilterCriteriaJson = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSettings_CreatedAt",
                table: "NotificationSettings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSettings_NotificationType",
                table: "NotificationSettings",
                column: "NotificationType");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSettings_NotifyOnDeadlineApproaching",
                table: "NotificationSettings",
                column: "NotifyOnDeadlineApproaching");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSettings_NotifyOnNewTenders",
                table: "NotificationSettings",
                column: "NotifyOnNewTenders");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSettings_NotifyOnTechnologyMatch",
                table: "NotificationSettings",
                column: "NotifyOnTechnologyMatch");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSettings_UpdatedAt",
                table: "NotificationSettings",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSettings_UserId",
                table: "NotificationSettings",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationSettings");
        }
    }
}
