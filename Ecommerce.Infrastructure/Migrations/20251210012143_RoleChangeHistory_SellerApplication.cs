using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RoleChangeHistory_SellerApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Role_Change_History",
                columns: table => new
                {
                    ChangeHistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NewRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role_Change_History", x => x.ChangeHistoryId);
                    table.ForeignKey(
                        name: "FK_Role_Change_History_ApplicationUsers_ChangeBy",
                        column: x => x.ChangeBy,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Role_Change_History_ApplicationUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Role_Change_History_AspNetRoles_NewRoleId",
                        column: x => x.NewRoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Role_Change_History_AspNetRoles_OldRoleId",
                        column: x => x.OldRoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Seller-Applications",
                columns: table => new
                {
                    SellerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApplicationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewComments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seller-Applications", x => x.SellerId);
                    table.CheckConstraint("CK_Application_Status", "[Status] IN ('Pending', 'Approved', 'Rejected')");
                    table.ForeignKey(
                        name: "FK_Seller-Applications_ApplicationUsers_ReviewBy",
                        column: x => x.ReviewBy,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Role_Change_History_ChangeBy",
                table: "Role_Change_History",
                column: "ChangeBy");

            migrationBuilder.CreateIndex(
                name: "IX_Role_Change_History_ChangeHistoryId",
                table: "Role_Change_History",
                column: "ChangeHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Role_Change_History_NewRoleId",
                table: "Role_Change_History",
                column: "NewRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Role_Change_History_OldRoleId",
                table: "Role_Change_History",
                column: "OldRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Role_Change_History_UserId",
                table: "Role_Change_History",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Seller-Applications_ApplicationReason",
                table: "Seller-Applications",
                column: "ApplicationReason");

            migrationBuilder.CreateIndex(
                name: "IX_Seller-Applications_BusinessName",
                table: "Seller-Applications",
                column: "BusinessName");

            migrationBuilder.CreateIndex(
                name: "IX_Seller-Applications_ReviewBy",
                table: "Seller-Applications",
                column: "ReviewBy");

            migrationBuilder.CreateIndex(
                name: "IX_Seller-Applications_SellerId",
                table: "Seller-Applications",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_Seller-Applications_Status",
                table: "Seller-Applications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Seller-Applications_SubmittedAt",
                table: "Seller-Applications",
                column: "SubmittedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Role_Change_History");

            migrationBuilder.DropTable(
                name: "Seller-Applications");
        }
    }
}
