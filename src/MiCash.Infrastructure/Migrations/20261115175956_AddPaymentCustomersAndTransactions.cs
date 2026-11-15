using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MiCash.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentCustomersAndTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Order matters: new tables first, then the existing ids are copied, and only then the old columns go.
            migrationBuilder.AddColumn<long>(
                name: "payment_customer_id",
                table: "subscriptions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "payment_customers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    provider = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    external_customer_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    first_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_customers", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_customers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_transactions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    subscription_id = table.Column<long>(type: "bigint", nullable: false),
                    external_charge_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    amount_cents = table.Column<long>(type: "bigint", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    refunded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_transactions_subscriptions_subscription_id",
                        column: x => x.subscription_id,
                        principalTable: "subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_payment_customer_id",
                table: "subscriptions",
                column: "payment_customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_customers_user_id_provider_external_customer_id",
                table: "payment_customers",
                columns: new[] { "user_id", "provider", "external_customer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_external_charge_id",
                table: "payment_transactions",
                column: "external_charge_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_subscription_id",
                table: "payment_transactions",
                column: "subscription_id");

            migrationBuilder.AddForeignKey(
                name: "FK_subscriptions_payment_customers_payment_customer_id",
                table: "subscriptions",
                column: "payment_customer_id",
                principalTable: "payment_customers",
                principalColumn: "id");

            // One payer per (user, gateway customer) already used by the user's subscriptions...
            migrationBuilder.Sql("""
                INSERT INTO payment_customers (user_id, provider, external_customer_id, first_used_at, last_used_at, created_at, updated_at)
                SELECT s.user_id, 'pagarme', s.pagarme_customer_id, MIN(s.created_at), MAX(s.created_at), NOW(), NOW()
                FROM subscriptions s
                WHERE s.pagarme_customer_id IS NOT NULL AND s.pagarme_customer_id <> ''
                GROUP BY s.user_id, s.pagarme_customer_id;
                """);

            // ...plus the last payer kept on the user, if no subscription row has it.
            migrationBuilder.Sql("""
                INSERT INTO payment_customers (user_id, provider, external_customer_id, first_used_at, last_used_at, created_at, updated_at)
                SELECT u.id, 'pagarme', u.pagarme_customer_id, u.updated_at, u.updated_at, NOW(), NOW()
                FROM users u
                WHERE u.pagarme_customer_id IS NOT NULL AND u.pagarme_customer_id <> ''
                  AND NOT EXISTS (
                      SELECT 1 FROM payment_customers pc
                      WHERE pc.user_id = u.id AND pc.provider = 'pagarme' AND pc.external_customer_id = u.pagarme_customer_id);
                """);

            migrationBuilder.Sql("""
                UPDATE subscriptions s
                SET payment_customer_id = pc.id
                FROM payment_customers pc
                WHERE pc.user_id = s.user_id AND pc.provider = 'pagarme' AND pc.external_customer_id = s.pagarme_customer_id;
                """);

            // Only the last charge of each subscription was kept. Legacy rows: amount from the plan price, no dates
            // (unknown), status from the subscription (the last charge failed when the subscription is PaymentFailed).
            migrationBuilder.Sql("""
                INSERT INTO payment_transactions (subscription_id, external_charge_id, amount_cents, status, created_at, updated_at)
                SELECT s.id, s.pagarme_charge_id, ROUND(p.price * 100)::bigint,
                       CASE WHEN s.status = 'PaymentFailed' THEN 'Failed' ELSE 'Paid' END,
                       s.updated_at, s.updated_at
                FROM subscriptions s
                JOIN plans p ON p.id = s.plan_id
                WHERE s.pagarme_charge_id IS NOT NULL AND s.pagarme_charge_id <> ''
                ON CONFLICT (external_charge_id) DO NOTHING;
                """);

            migrationBuilder.DropIndex(
                name: "IX_users_pagarme_customer_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "pagarme_customer_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "pagarme_charge_id",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "pagarme_customer_id",
                table: "subscriptions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "pagarme_customer_id",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pagarme_charge_id",
                table: "subscriptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pagarme_customer_id",
                table: "subscriptions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_pagarme_customer_id",
                table: "users",
                column: "pagarme_customer_id",
                unique: false);

            // Restores what the old columns can hold: the payer of each subscription, its latest charge, and the
            // user's latest payer (the old unique index is recreated as non-unique: one payer may now pay several users).
            migrationBuilder.Sql("""
                UPDATE subscriptions s SET pagarme_customer_id = pc.external_customer_id
                FROM payment_customers pc WHERE pc.id = s.payment_customer_id;

                UPDATE subscriptions s SET pagarme_charge_id = t.external_charge_id
                FROM (SELECT DISTINCT ON (subscription_id) subscription_id, external_charge_id
                      FROM payment_transactions ORDER BY subscription_id, created_at DESC) t
                WHERE t.subscription_id = s.id;

                UPDATE users u SET pagarme_customer_id = pc.external_customer_id
                FROM (SELECT DISTINCT ON (user_id) user_id, external_customer_id
                      FROM payment_customers ORDER BY user_id, last_used_at DESC) pc
                WHERE pc.user_id = u.id;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_subscriptions_payment_customers_payment_customer_id",
                table: "subscriptions");

            migrationBuilder.DropTable(
                name: "payment_transactions");

            migrationBuilder.DropTable(
                name: "payment_customers");

            migrationBuilder.DropIndex(
                name: "IX_subscriptions_payment_customer_id",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "payment_customer_id",
                table: "subscriptions");
        }
    }
}
