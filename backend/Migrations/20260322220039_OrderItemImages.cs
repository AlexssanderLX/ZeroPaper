using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class OrderItemImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "orderitems",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(
                """
                UPDATE orderitems oi
                INNER JOIN customerorders co ON co.Id = oi.CustomerOrderId
                INNER JOIN menuitems mi
                    ON mi.CompanyId = co.CompanyId
                    AND LOWER(TRIM(mi.Name)) = LOWER(TRIM(oi.Name))
                LEFT JOIN menucategories mc ON mc.Id = mi.MenuCategoryId
                SET oi.ImageUrl = mi.ImageUrl
                WHERE oi.ImageUrl IS NULL
                  AND mi.ImageUrl IS NOT NULL
                  AND (
                      oi.CategoryName IS NULL
                      OR LOWER(TRIM(oi.CategoryName)) = LOWER(TRIM(mc.Name))
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "orderitems");
        }
    }
}
