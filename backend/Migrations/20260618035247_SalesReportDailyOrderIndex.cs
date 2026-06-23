using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class SalesReportDailyOrderIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_customerorders_CompanyId_SubmittedAtUtc_Status_PaymentStatus",
                table: "customerorders",
                columns: new[] { "CompanyId", "SubmittedAtUtc", "Status", "PaymentStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_customerorders_CompanyId_SubmittedAtUtc_Status_PaymentStatus",
                table: "customerorders");
        }
    }
}
