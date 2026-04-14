using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WeblogApplication.Data;

#nullable disable

namespace Weblog_Application.Migrations
{
    [DbContext(typeof(WeblogApplicationDbContext))]
    [Migration("20260414033300_BackfillPublishedBlogs")]
    public partial class BackfillPublishedBlogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Blogs] SET [Published] = 1 WHERE [Published] = 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}