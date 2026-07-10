using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesAgents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SalesAgentId",
                table: "customerorders",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<int>(
                name: "SalesOrigin",
                table: "customerorders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "salesagents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CompanyId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommissionPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salesagents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_salesagents_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_customerorders_SalesAgentId",
                table: "customerorders",
                column: "SalesAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_salesagents_Code",
                table: "salesagents",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_salesagents_CompanyId_IsActive",
                table: "salesagents",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_customerorders_salesagents_SalesAgentId",
                table: "customerorders",
                column: "SalesAgentId",
                principalTable: "salesagents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_customerorders_salesagents_SalesAgentId",
                table: "customerorders");

            migrationBuilder.DropTable(
                name: "salesagents");

            migrationBuilder.DropIndex(
                name: "IX_customerorders_SalesAgentId",
                table: "customerorders");

            migrationBuilder.DropColumn(
                name: "SalesAgentId",
                table: "customerorders");

            migrationBuilder.DropColumn(
                name: "SalesOrigin",
                table: "customerorders");
        }
    }
}
