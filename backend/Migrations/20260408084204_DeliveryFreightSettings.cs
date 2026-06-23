using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class DeliveryFreightSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryDistanceKm",
                table: "customerorders",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryFreightAmount",
                table: "customerorders",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryFreightCalculatedAtUtc",
                table: "customerorders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryFreightProvider",
                table: "customerorders",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryPostalCode",
                table: "customerorders",
                type: "varchar(8)",
                maxLength: 8,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryFreightBaseFee",
                table: "companies",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryFreightPricePerKm",
                table: "companies",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryOriginPostalCode",
                table: "companies",
                type: "varchar(8)",
                maxLength: 8,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "EnableDeliveryFreight",
                table: "companies",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "deliverydistancecaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CompanyId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    OriginPostalCode = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DestinationPostalCode = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Provider = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DistanceKm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deliverydistancecaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_deliverydistancecaches_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_deliverydistancecaches_CompanyId_Provider_OriginPostalCode_D~",
                table: "deliverydistancecaches",
                columns: new[] { "CompanyId", "Provider", "OriginPostalCode", "DestinationPostalCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_deliverydistancecaches_ExpiresAtUtc",
                table: "deliverydistancecaches",
                column: "ExpiresAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deliverydistancecaches");

            migrationBuilder.DropColumn(
                name: "DeliveryDistanceKm",
                table: "customerorders");

            migrationBuilder.DropColumn(
                name: "DeliveryFreightAmount",
                table: "customerorders");

            migrationBuilder.DropColumn(
                name: "DeliveryFreightCalculatedAtUtc",
                table: "customerorders");

            migrationBuilder.DropColumn(
                name: "DeliveryFreightProvider",
                table: "customerorders");

            migrationBuilder.DropColumn(
                name: "DeliveryPostalCode",
                table: "customerorders");

            migrationBuilder.DropColumn(
                name: "DeliveryFreightBaseFee",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "DeliveryFreightPricePerKm",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "DeliveryOriginPostalCode",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "EnableDeliveryFreight",
                table: "companies");
        }
    }
}
