using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class MercadoPagoPublicSubscriptionCheckout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MercadoPagoPreapprovalPlanId",
                table: "subscriptions",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MercadoPagoPreapprovalPlanId",
                table: "subscriptions");
        }
    }
}
