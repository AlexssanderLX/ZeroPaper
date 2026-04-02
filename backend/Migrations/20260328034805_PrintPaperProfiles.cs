using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class PrintPaperProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrintOrdersPerPage",
                table: "companies",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "PrintPaperProfile",
                table: "companies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE companies
                SET PrintOrdersPerPage = 1
                WHERE PrintOrdersPerPage IS NULL OR PrintOrdersPerPage < 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrintOrdersPerPage",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "PrintPaperProfile",
                table: "companies");
        }
    }
}
