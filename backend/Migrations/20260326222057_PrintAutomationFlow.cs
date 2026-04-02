using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class PrintAutomationFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrintAgentName",
                table: "customerorders",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "PrintAttempts",
                table: "customerorders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrintClaimedAtUtc",
                table: "customerorders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrintLastError",
                table: "customerorders",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PrintPrinterName",
                table: "customerorders",
                type: "varchar(180)",
                maxLength: 180,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "PrintQueuedAtUtc",
                table: "customerorders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrintStatus",
                table: "customerorders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrintedAtUtc",
                table: "customerorders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableAutomaticPrinting",
                table: "companies",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PrintAgentKeyHash",
                table: "companies",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "PrintAgentLastSeenAtUtc",
                table: "companies",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrintAgentName",
                table: "companies",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PrintAgentPrinterName",
                table: "companies",
                type: "varchar(180)",
                maxLength: 180,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrintAgentName",
                table: "customerorders");

            migrationBuilder.DropColumn(
                name: "PrintAttempts",
                table: "customerorders");

            migrationBuilder.DropColumn(
                name: "PrintClaimedAtUtc",
                table: "customerorders");

            migrationBuilder.DropColumn(
                name: "PrintLastError",
                table: "customerorders");

            migrationBuilder.DropColumn(
                name: "PrintPrinterName",
                table: "customerorders");

            migrationBuilder.DropColumn(
                name: "PrintQueuedAtUtc",
                table: "customerorders");

            migrationBuilder.DropColumn(
                name: "PrintStatus",
                table: "customerorders");

            migrationBuilder.DropColumn(
                name: "PrintedAtUtc",
                table: "customerorders");

            migrationBuilder.DropColumn(
                name: "EnableAutomaticPrinting",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "PrintAgentKeyHash",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "PrintAgentLastSeenAtUtc",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "PrintAgentName",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "PrintAgentPrinterName",
                table: "companies");
        }
    }
}
