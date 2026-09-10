using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class RestaurantDemoAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DemoAccessLinkId",
                table: "sessions",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "BillingExemptChangedAtUtc",
                table: "companies",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BillingExemptChangedByUserId",
                table: "companies",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<bool>(
                name: "IsBillingExempt",
                table: "companies",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDemoAccount",
                table: "companies",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "demoaccesslinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CompanyId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AppUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedByUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TokenHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastUsedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_demoaccesslinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_demoaccesslinks_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_demoaccesslinks_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_demoaccesslinks_users_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_demoaccesslinks_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_DemoAccessLinkId",
                table: "sessions",
                column: "DemoAccessLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_demoaccesslinks_AppUserId",
                table: "demoaccesslinks",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_demoaccesslinks_CompanyId_IsActive",
                table: "demoaccesslinks",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_demoaccesslinks_CreatedByUserId",
                table: "demoaccesslinks",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_demoaccesslinks_TenantId",
                table: "demoaccesslinks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_demoaccesslinks_TokenHash",
                table: "demoaccesslinks",
                column: "TokenHash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_sessions_demoaccesslinks_DemoAccessLinkId",
                table: "sessions",
                column: "DemoAccessLinkId",
                principalTable: "demoaccesslinks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sessions_demoaccesslinks_DemoAccessLinkId",
                table: "sessions");

            migrationBuilder.DropTable(
                name: "demoaccesslinks");

            migrationBuilder.DropIndex(
                name: "IX_sessions_DemoAccessLinkId",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "DemoAccessLinkId",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "BillingExemptChangedAtUtc",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "BillingExemptChangedByUserId",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "IsBillingExempt",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "IsDemoAccount",
                table: "companies");
        }
    }
}
