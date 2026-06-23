using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class DeliveryCustomerProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "deliverycustomerprofiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CompanyId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Phone = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerName = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeliveryAddress = table.Column<string>(type: "varchar(220)", maxLength: 220, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeliveryNumber = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeliveryComplement = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeliveryPostalCode = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastOrderAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deliverycustomerprofiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_deliverycustomerprofiles_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_deliverycustomerprofiles_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_deliverycustomerprofiles_CompanyId_LastOrderAtUtc",
                table: "deliverycustomerprofiles",
                columns: new[] { "CompanyId", "LastOrderAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_deliverycustomerprofiles_CompanyId_Phone",
                table: "deliverycustomerprofiles",
                columns: new[] { "CompanyId", "Phone" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_deliverycustomerprofiles_TenantId",
                table: "deliverycustomerprofiles",
                column: "TenantId");

            migrationBuilder.Sql(
                """
                INSERT IGNORE INTO `deliverycustomerprofiles`
                    (`Id`, `TenantId`, `CompanyId`, `Phone`, `CustomerName`, `DeliveryAddress`, `DeliveryNumber`, `DeliveryComplement`, `DeliveryPostalCode`, `LastOrderAtUtc`, `CreatedAtUtc`, `UpdatedAtUtc`, `IsActive`)
                SELECT
                    UUID(),
                    ranked.`TenantId`,
                    ranked.`CompanyId`,
                    ranked.`PhoneNormalized`,
                    ranked.`CustomerName`,
                    ranked.`DeliveryAddress`,
                    ranked.`DeliveryNumber`,
                    ranked.`DeliveryComplement`,
                    ranked.`DeliveryPostalCode`,
                    ranked.`SubmittedAtUtc`,
                    UTC_TIMESTAMP(6),
                    UTC_TIMESTAMP(6),
                    TRUE
                FROM (
                    SELECT
                        normalized.*,
                        ROW_NUMBER() OVER (
                            PARTITION BY normalized.`CompanyId`, normalized.`PhoneNormalized`
                            ORDER BY normalized.`SubmittedAtUtc` DESC, normalized.`Id` DESC
                        ) AS `RowNumber`
                    FROM (
                        SELECT
                            clean.`Id`,
                            clean.`TenantId`,
                            clean.`CompanyId`,
                            CASE
                                WHEN CHAR_LENGTH(clean.`PhoneDigits`) IN (10, 11) THEN CONCAT('55', clean.`PhoneDigits`)
                                ELSE clean.`PhoneDigits`
                            END AS `PhoneNormalized`,
                            clean.`CustomerName`,
                            clean.`DeliveryAddress`,
                            clean.`DeliveryNumber`,
                            clean.`DeliveryComplement`,
                            clean.`DeliveryPostalCode`,
                            clean.`SubmittedAtUtc`
                        FROM (
                            SELECT
                                orders.`Id`,
                                orders.`TenantId`,
                                orders.`CompanyId`,
                                REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(TRIM(orders.`DeliveryPhone`), ' ', ''), '-', ''), '(', ''), ')', ''), '+', ''), '.', '') AS `PhoneDigits`,
                                orders.`CustomerName`,
                                orders.`DeliveryAddress`,
                                orders.`DeliveryNumber`,
                                orders.`DeliveryComplement`,
                                orders.`DeliveryPostalCode`,
                                orders.`SubmittedAtUtc`
                            FROM `customerorders` orders
                            INNER JOIN `diningtables` tables ON tables.`Id` = orders.`DiningTableId`
                            WHERE orders.`IsActive` = TRUE
                              AND orders.`DeliveryPhone` IS NOT NULL
                              AND tables.`IsDeliveryChannel` = TRUE
                        ) clean
                    ) normalized
                    WHERE normalized.`PhoneNormalized` <> ''
                ) ranked
                WHERE ranked.`RowNumber` = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deliverycustomerprofiles");
        }
    }
}
