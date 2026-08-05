using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class SubscriptionPaymentAccessPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CheckoutConfirmationTokenHash",
                table: "subscriptions",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidThroughUtc",
                table: "subscriptions",
                type: "datetime(6)",
                nullable: true);

            // Preserve access for existing active customers during the transition.
            // Pending pre-registrations remain unpaid and blocked.
            migrationBuilder.Sql("""
                UPDATE subscriptions s
                INNER JOIN users u ON u.TenantId = s.TenantId AND u.Role = 1 AND u.IsActive = TRUE
                SET s.PaidThroughUtc = DATE_ADD(UTC_TIMESTAMP(6), INTERVAL 1 MONTH)
                WHERE s.IsActive = TRUE AND s.PaidThroughUtc IS NULL;
                """);

            migrationBuilder.CreateTable(
                name: "subscriptionpayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SubscriptionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Source = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExternalPaymentId = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ConfirmedByUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptionpayments", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptionpayments_Source_ExternalPaymentId",
                table: "subscriptionpayments",
                columns: new[] { "Source", "ExternalPaymentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscriptionpayments_SubscriptionId_PaidAtUtc",
                table: "subscriptionpayments",
                columns: new[] { "SubscriptionId", "PaidAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "subscriptionpayments");

            migrationBuilder.DropColumn(
                name: "CheckoutConfirmationTokenHash",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "PaidThroughUtc",
                table: "subscriptions");
        }
    }
}
