using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiCash.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeProcessedWebhookIdUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Concurrent deliveries could have inserted the same webhook twice: keep only the latest row,
            // otherwise the unique index cannot be created (and the startup migration would fail).
            migrationBuilder.Sql(@"
                DELETE FROM processed_webhook older
                USING processed_webhook newer
                WHERE older.webhook_id = newer.webhook_id
                  AND older.id < newer.id;");

            migrationBuilder.DropIndex(
                name: "IX_processed_webhook_webhook_id",
                table: "processed_webhook");

            migrationBuilder.CreateIndex(
                name: "IX_processed_webhook_webhook_id",
                table: "processed_webhook",
                column: "webhook_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_processed_webhook_webhook_id",
                table: "processed_webhook");

            migrationBuilder.CreateIndex(
                name: "IX_processed_webhook_webhook_id",
                table: "processed_webhook",
                column: "webhook_id");
        }
    }
}
