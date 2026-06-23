using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class CustomerProfileOrderHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryNeighborhood",
                table: "deliverycustomerprofiles",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "customerorderhistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CompanyId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CustomerProfileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    OrderId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TotalAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customerorderhistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customerorderhistories_deliverycustomerprofiles_CustomerProf~",
                        column: x => x.CustomerProfileId,
                        principalTable: "deliverycustomerprofiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_customerorderhistories_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "customerorderhistoryitems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CustomerOrderHistoryId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ItemName = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customerorderhistoryitems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customerorderhistoryitems_customerorderhistories_CustomerOrd~",
                        column: x => x.CustomerOrderHistoryId,
                        principalTable: "customerorderhistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_customerorderhistoryitems_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_customerorderhistories_CompanyId_CustomerProfileId_CreatedAt~",
                table: "customerorderhistories",
                columns: new[] { "CompanyId", "CustomerProfileId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_customerorderhistories_CustomerProfileId",
                table: "customerorderhistories",
                column: "CustomerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_customerorderhistories_OrderId",
                table: "customerorderhistories",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customerorderhistories_TenantId",
                table: "customerorderhistories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_customerorderhistoryitems_CustomerOrderHistoryId",
                table: "customerorderhistoryitems",
                column: "CustomerOrderHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_customerorderhistoryitems_TenantId",
                table: "customerorderhistoryitems",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customerorderhistoryitems");

            migrationBuilder.DropTable(
                name: "customerorderhistories");

            migrationBuilder.DropColumn(
                name: "DeliveryNeighborhood",
                table: "deliverycustomerprofiles");
        }
    }
}
