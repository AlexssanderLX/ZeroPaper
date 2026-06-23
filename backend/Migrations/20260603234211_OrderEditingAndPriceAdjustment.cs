using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class OrderEditingAndPriceAdjustment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "customerorders",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "EditedAtUtc",
                table: "customerorders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEdited",
                table: "customerorders",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PriceAdjustedAtUtc",
                table: "customerorders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceAdjustmentNote",
                table: "customerorders",
                type: "varchar(240)",
                maxLength: 240,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "SurchargeAmount",
                table: "customerorders",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "customerorders");

            migrationBuilder.DropColumn(
                name: "EditedAtUtc",
                table: "customerorders");

            migrationBuilder.DropColumn(
                name: "IsEdited",
                table: "customerorders");

            migrationBuilder.DropColumn(
                name: "PriceAdjustedAtUtc",
                table: "customerorders");

            migrationBuilder.DropColumn(
                name: "PriceAdjustmentNote",
                table: "customerorders");

            migrationBuilder.DropColumn(
                name: "SurchargeAmount",
                table: "customerorders");
        }
    }
}
