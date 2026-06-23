using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class DeliveryChannelTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeliveryChannel",
                table: "diningtables",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_diningtables_CompanyId_IsDeliveryChannel",
                table: "diningtables",
                columns: new[] { "CompanyId", "IsDeliveryChannel" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_diningtables_CompanyId_IsDeliveryChannel",
                table: "diningtables");

            migrationBuilder.DropColumn(
                name: "IsDeliveryChannel",
                table: "diningtables");
        }
    }
}
