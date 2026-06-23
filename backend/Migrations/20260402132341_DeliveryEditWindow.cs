using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class DeliveryEditWindow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceMenuItemId",
                table: "orderitems",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "PublicEditAllowedUntilUtc",
                table: "customerorders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicEditCode",
                table: "customerorders",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_customerorders_PublicEditCode",
                table: "customerorders",
                column: "PublicEditCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_customerorders_PublicEditCode",
                table: "customerorders");

            migrationBuilder.DropColumn(
                name: "SourceMenuItemId",
                table: "orderitems");

            migrationBuilder.DropColumn(
                name: "PublicEditAllowedUntilUtc",
                table: "customerorders");

            migrationBuilder.DropColumn(
                name: "PublicEditCode",
                table: "customerorders");
        }
    }
}
