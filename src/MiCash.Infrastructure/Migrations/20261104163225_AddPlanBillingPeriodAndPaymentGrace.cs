using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiCash.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanBillingPeriodAndPaymentGrace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "payment_grace_until",
                table: "subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "billing_period",
                table: "plans",
                type: "text",
                nullable: false,
                // Enums are stored as text: the plans that exist today are monthly.
                defaultValue: "Monthly");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "payment_grace_until",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "billing_period",
                table: "plans");
        }
    }
}
