using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class SubscriptionFeatureModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IncludesAiAssistantModule",
                table: "subscriptions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncludesCashModule",
                table: "subscriptions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncludesDeliveryModule",
                table: "subscriptions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncludesKitchenModule",
                table: "subscriptions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncludesMenuModule",
                table: "subscriptions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncludesPrintingModule",
                table: "subscriptions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncludesStockModule",
                table: "subscriptions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncludesTablesModule",
                table: "subscriptions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncludesWaiterCallModule",
                table: "subscriptions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql("""
                UPDATE subscriptions
                SET IncludesMenuModule = 1,
                    IncludesTablesModule = 1,
                    IncludesKitchenModule = 1,
                    IncludesCashModule = 1,
                    IncludesStockModule = 1,
                    IncludesDeliveryModule = 1,
                    IncludesPrintingModule = 1,
                    IncludesWaiterCallModule = 1
                WHERE IsActive = 1;
                """);

            migrationBuilder.Sql("""
                UPDATE subscriptions
                SET MonthlyPrice = 79.90,
                    PlanName = 'ZeroPaper Operacao'
                WHERE IsActive = 1
                  AND (MonthlyPrice = 0 OR PlanName = 'ZeroPaper Base' OR PlanName = 'Plano nao informado');
                """);

            migrationBuilder.Sql("""
                UPDATE subscriptions s
                INNER JOIN companies c ON c.TenantId = s.TenantId
                SET s.IncludesAiAssistantModule = 1,
                    s.MonthlyPrice = 119.90,
                    s.PlanName = 'ZeroPaper Operacao + IA'
                WHERE s.IsActive = 1
                  AND c.EnableAiAssistant = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IncludesAiAssistantModule",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "IncludesCashModule",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "IncludesDeliveryModule",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "IncludesKitchenModule",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "IncludesMenuModule",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "IncludesPrintingModule",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "IncludesStockModule",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "IncludesTablesModule",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "IncludesWaiterCallModule",
                table: "subscriptions");
        }
    }
}
