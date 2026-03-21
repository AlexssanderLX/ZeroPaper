using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class StableOrderSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastOrderNumber",
                table: "Companies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE Companies company
                LEFT JOIN (
                    SELECT CompanyId, COALESCE(MAX(Number), 0) AS MaxNumber
                    FROM CustomerOrders
                    GROUP BY CompanyId
                ) orders ON orders.CompanyId = company.Id
                SET company.LastOrderNumber = COALESCE(orders.MaxNumber, 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastOrderNumber",
                table: "Companies");
        }
    }
}
