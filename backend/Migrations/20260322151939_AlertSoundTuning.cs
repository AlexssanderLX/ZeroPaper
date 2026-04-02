using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class AlertSoundTuning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AlertPlaybackSeconds",
                table: "companies",
                type: "int",
                nullable: false,
                defaultValue: 6);

            migrationBuilder.AddColumn<int>(
                name: "AlertVolumePercent",
                table: "companies",
                type: "int",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.Sql("""
                UPDATE companies
                SET AlertPlaybackSeconds = 6,
                    AlertVolumePercent = 100
                WHERE AlertPlaybackSeconds = 0
                   OR AlertVolumePercent = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlertPlaybackSeconds",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "AlertVolumePercent",
                table: "companies");
        }
    }
}
