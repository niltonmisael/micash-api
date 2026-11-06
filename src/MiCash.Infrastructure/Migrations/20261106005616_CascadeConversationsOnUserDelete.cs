using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiCash.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CascadeConversationsOnUserDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_conversations_users_userId",
                table: "conversations");

            migrationBuilder.AddForeignKey(
                name: "FK_conversations_users_userId",
                table: "conversations",
                column: "userId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_conversations_users_userId",
                table: "conversations");

            migrationBuilder.AddForeignKey(
                name: "FK_conversations_users_userId",
                table: "conversations",
                column: "userId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
