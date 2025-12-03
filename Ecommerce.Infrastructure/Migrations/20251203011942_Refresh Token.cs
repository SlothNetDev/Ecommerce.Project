using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationToken_ApplicationUsers_UserId",
                table: "ApplicationToken");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApplicationToken",
                table: "ApplicationToken");

            migrationBuilder.RenameTable(
                name: "ApplicationToken",
                newName: "RefrehTokens");

            migrationBuilder.RenameIndex(
                name: "IX_ApplicationToken_UserId",
                table: "RefrehTokens",
                newName: "IX_RefrehTokens_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RefrehTokens",
                table: "RefrehTokens",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RefrehTokens_ApplicationUsers_UserId",
                table: "RefrehTokens",
                column: "UserId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefrehTokens_ApplicationUsers_UserId",
                table: "RefrehTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RefrehTokens",
                table: "RefrehTokens");

            migrationBuilder.RenameTable(
                name: "RefrehTokens",
                newName: "ApplicationToken");

            migrationBuilder.RenameIndex(
                name: "IX_RefrehTokens_UserId",
                table: "ApplicationToken",
                newName: "IX_ApplicationToken_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApplicationToken",
                table: "ApplicationToken",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationToken_ApplicationUsers_UserId",
                table: "ApplicationToken",
                column: "UserId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
