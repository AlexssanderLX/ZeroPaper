using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class SubscriptionProductType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductType",
                table: "subscriptions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(
                """
                UPDATE subscriptions AS s
                INNER JOIN companies AS c ON c.TenantId = s.TenantId
                SET s.ProductType = 2,
                    s.PlanName = 'ZeroPaper Pet Shop',
                    s.MonthlyPrice = 150.00,
                    s.IncludesMenuModule = TRUE,
                    s.IncludesTablesModule = FALSE,
                    s.IncludesKitchenModule = FALSE,
                    s.IncludesCashModule = TRUE,
                    s.IncludesStockModule = FALSE,
                    s.IncludesDeliveryModule = FALSE,
                    s.IncludesPrintingModule = FALSE,
                    s.IncludesWaiterCallModule = FALSE,
                    s.IncludesAiAssistantModule = FALSE
                WHERE c.BusinessSegment = 2
                  AND NOT EXISTS (
                      SELECT 1
                      FROM companies AS other_company
                      WHERE other_company.TenantId = s.TenantId
                        AND other_company.BusinessSegment <> 2
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "subscriptions");
        }
    }
}
