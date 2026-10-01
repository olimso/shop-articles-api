using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopArticles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "articles",
                type: "INTEGER",
                nullable: false,
                // Existing articles start at version 1, like newly created ones.
                defaultValue: 1L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "version",
                table: "articles");
        }
    }
}
