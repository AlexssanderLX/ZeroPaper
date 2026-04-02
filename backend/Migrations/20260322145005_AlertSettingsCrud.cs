using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class AlertSettingsCrud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlertSoundUrl",
                table: "companies",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "EnableOrderAlerts",
                table: "companies",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableWaiterCallAlerts",
                table: "companies",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql("UPDATE companies SET EnableOrderAlerts = 1, EnableWaiterCallAlerts = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlertSoundUrl",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "EnableOrderAlerts",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "EnableWaiterCallAlerts",
                table: "companies");
        }
    }
}
