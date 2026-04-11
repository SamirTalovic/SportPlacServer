using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportPlac.Migrations
{
    /// <inheritdoc />
    public partial class Subsortorder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Subcategories",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Subcategories");
        }
    }
}
