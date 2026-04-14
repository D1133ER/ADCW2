using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Weblog_Application.Migrations
{
    /// <inheritdoc />
    public partial class AddBlogPublishingAndUserBio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Published",
                table: "Blogs",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql("UPDATE [Blogs] SET [Published] = 1 WHERE [Published] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bio",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Published",
                table: "Blogs");
        }
    }
}
