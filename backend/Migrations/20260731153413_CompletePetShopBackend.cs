using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class CompletePetShopBackend : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "AppointmentEndTime",
                table: "companies",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(18, 0, 0));

            migrationBuilder.AddColumn<string>(
                name: "AppointmentServiceDays",
                table: "companies",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "1,2,3,4,5,6")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "AppointmentSlotIntervalMinutes",
                table: "companies",
                type: "int",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "AppointmentStartTime",
                table: "companies",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(8, 0, 0));

            migrationBuilder.AddColumn<string>(
                name: "PetShopPublicCode",
                table: "companies",
                type: "varchar(48)",
                maxLength: 48,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "PublicAccessExpiresAtUtc",
                table: "appointments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublicAccessRevokedAtUtc",
                table: "appointments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicAccessTokenHash",
                table: "appointments",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "appointmentblocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CompanyId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AssignedUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    StartsAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Reason = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointmentblocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_appointmentblocks_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_appointmentblocks_users_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "appointmentstatushistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CompanyId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AppointmentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ChangedByUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PreviousStatus = table.Column<int>(type: "int", nullable: false),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointmentstatushistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_appointmentstatushistories_appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_appointmentstatushistories_users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_companies_PetShopPublicCode",
                table: "companies",
                column: "PetShopPublicCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_appointments_PublicAccessTokenHash",
                table: "appointments",
                column: "PublicAccessTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_appointmentblocks_AssignedUserId",
                table: "appointmentblocks",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_appointmentblocks_CompanyId_StartsAtUtc_EndsAtUtc",
                table: "appointmentblocks",
                columns: new[] { "CompanyId", "StartsAtUtc", "EndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_appointmentstatushistories_AppointmentId",
                table: "appointmentstatushistories",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_appointmentstatushistories_ChangedByUserId",
                table: "appointmentstatushistories",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_appointmentstatushistories_CompanyId_AppointmentId_ChangedAt~",
                table: "appointmentstatushistories",
                columns: new[] { "CompanyId", "AppointmentId", "ChangedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "appointmentblocks");

            migrationBuilder.DropTable(
                name: "appointmentstatushistories");

            migrationBuilder.DropIndex(
                name: "IX_companies_PetShopPublicCode",
                table: "companies");

            migrationBuilder.DropIndex(
                name: "IX_appointments_PublicAccessTokenHash",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "AppointmentEndTime",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "AppointmentServiceDays",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "AppointmentSlotIntervalMinutes",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "AppointmentStartTime",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "PetShopPublicCode",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "PublicAccessExpiresAtUtc",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "PublicAccessRevokedAtUtc",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "PublicAccessTokenHash",
                table: "appointments");
        }
    }
}
