using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class MercadoPagoConnect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMercadoPagoConnected",
                table: "companies",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MercadoPagoAccessTokenCipherText",
                table: "companies",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "MercadoPagoConnectedAtUtc",
                table: "companies",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MercadoPagoDisconnectedAtUtc",
                table: "companies",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MercadoPagoLiveMode",
                table: "companies",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MercadoPagoPublicKey",
                table: "companies",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MercadoPagoRefreshTokenCipherText",
                table: "companies",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "MercadoPagoTokenExpiresAtUtc",
                table: "companies",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MercadoPagoUserId",
                table: "companies",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMercadoPagoConnected",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "MercadoPagoAccessTokenCipherText",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "MercadoPagoConnectedAtUtc",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "MercadoPagoDisconnectedAtUtc",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "MercadoPagoLiveMode",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "MercadoPagoPublicKey",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "MercadoPagoRefreshTokenCipherText",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "MercadoPagoTokenExpiresAtUtc",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "MercadoPagoUserId",
                table: "companies");
        }
    }
}
