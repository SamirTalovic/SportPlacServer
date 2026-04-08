using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportPlac.Migrations
{
    /// <inheritdoc />
    public partial class Subcategoryrecursion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "Subcategories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subcategories_ParentId",
                table: "Subcategories",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subcategories_Subcategories_ParentId",
                table: "Subcategories",
                column: "ParentId",
                principalTable: "Subcategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subcategories_Subcategories_ParentId",
                table: "Subcategories");

            migrationBuilder.DropIndex(
                name: "IX_Subcategories_ParentId",
                table: "Subcategories");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Subcategories");
        }
    }
}
