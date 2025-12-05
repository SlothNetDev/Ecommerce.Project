using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateApplicationToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "RefrehTokens",
                newName: "TokenId");

            migrationBuilder.AddColumn<string>(
                name: "ReplacedByToken",
                table: "RefrehTokens",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReplacedByToken",
                table: "RefrehTokens");

            migrationBuilder.RenameColumn(
                name: "TokenId",
                table: "RefrehTokens",
                newName: "Id");
        }
    }
}
