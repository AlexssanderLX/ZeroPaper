using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class OwnerShortcutAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ShortcutAccessCreatedAtUtc",
                table: "users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShortcutAccessExpiresAtUtc",
                table: "users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShortcutAccessLastUsedAtUtc",
                table: "users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShortcutAccessRevokedAtUtc",
                table: "users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortcutAccessTokenHash",
                table: "users",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_users_ShortcutAccessTokenHash",
                table: "users",
                column: "ShortcutAccessTokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_ShortcutAccessTokenHash",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ShortcutAccessCreatedAtUtc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ShortcutAccessExpiresAtUtc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ShortcutAccessLastUsedAtUtc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ShortcutAccessRevokedAtUtc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ShortcutAccessTokenHash",
                table: "users");
        }
    }
}
