using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Weblog_Application.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetExpiry",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alert_BlogPostId",
                table: "Alert",
                column: "BlogPostId");

            migrationBuilder.AddForeignKey(
                name: "FK_Alert_Blogs_BlogPostId",
                table: "Alert",
                column: "BlogPostId",
                principalTable: "Blogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alert_Blogs_BlogPostId",
                table: "Alert");

            migrationBuilder.DropIndex(
                name: "IX_Alert_BlogPostId",
                table: "Alert");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetExpiry",
                table: "Users");
        }
    }
}
