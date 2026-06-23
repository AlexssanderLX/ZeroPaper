using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class DeliveryCustomerShortLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicAccessCodeCipherText",
                table: "deliverycustomerprofiles",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "PublicAccessCodeCreatedAtUtc",
                table: "deliverycustomerprofiles",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicAccessCodeHash",
                table: "deliverycustomerprofiles",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_deliverycustomerprofiles_PublicAccessCodeHash",
                table: "deliverycustomerprofiles",
                column: "PublicAccessCodeHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_deliverycustomerprofiles_PublicAccessCodeHash",
                table: "deliverycustomerprofiles");

            migrationBuilder.DropColumn(
                name: "PublicAccessCodeCipherText",
                table: "deliverycustomerprofiles");

            migrationBuilder.DropColumn(
                name: "PublicAccessCodeCreatedAtUtc",
                table: "deliverycustomerprofiles");

            migrationBuilder.DropColumn(
                name: "PublicAccessCodeHash",
                table: "deliverycustomerprofiles");
        }
    }
}
