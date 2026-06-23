using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class DeliveryEstimatedWaitTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryEstimatedMinutes",
                table: "companies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PickupEstimatedMinutes",
                table: "companies",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryEstimatedMinutes",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "PickupEstimatedMinutes",
                table: "companies");
        }
    }
}
