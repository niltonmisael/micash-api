using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiCash.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing users: monthly summary on (e-mail and push), news off (opt-in). New users get the same
            // values from the entity; the model has no database default on purpose (see UserConfiguration).
            migrationBuilder.AddColumn<bool>(
                name: "monthly_summary_email",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "monthly_summary_push",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "monthly_summary_sent_for",
                table: "users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "news_email",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "monthly_summary_email",
                table: "users");

            migrationBuilder.DropColumn(
                name: "monthly_summary_push",
                table: "users");

            migrationBuilder.DropColumn(
                name: "monthly_summary_sent_for",
                table: "users");

            migrationBuilder.DropColumn(
                name: "news_email",
                table: "users");
        }
    }
}
