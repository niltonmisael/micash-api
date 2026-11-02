using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiCash.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueActiveConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing data may already have more than one active conversation per user
            // (no constraint before). Keep only the newest active one, otherwise the unique
            // index below fails and, since migrations run at startup, the API would not start.
            migrationBuilder.Sql(
                """
                UPDATE conversations AS c
                SET is_active = false,
                    updated_at = now()
                WHERE c.is_active = true
                  AND EXISTS (
                      SELECT 1
                      FROM conversations AS newer
                      WHERE newer."userId" = c."userId"
                        AND newer.is_active = true
                        AND newer.id > c.id
                  );
                """);

            migrationBuilder.CreateIndex(
                name: "ux_conversations_user_active",
                table: "conversations",
                column: "userId",
                unique: true,
                filter: "\"is_active\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_conversations_user_active",
                table: "conversations");
        }
    }
}
