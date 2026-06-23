using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class WhatsAppAssistantIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableWhatsAppAssistant",
                table: "companies",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsWhatsAppConnected",
                table: "companies",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppAccountSecurityTokenCipherText",
                table: "companies",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "WhatsAppConnectedAtUtc",
                table: "companies",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppConnectedPhone",
                table: "companies",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "WhatsAppDisconnectedAtUtc",
                table: "companies",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppInstanceId",
                table: "companies",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppInstanceTokenCipherText",
                table: "companies",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "WhatsAppLastIncomingAtUtc",
                table: "companies",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WhatsAppLastOutgoingAtUtc",
                table: "companies",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppWebhookSecretCipherText",
                table: "companies",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "whatsappconversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CompanyId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ExternalPhone = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerName = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastMessagePreview = table.Column<string>(type: "varchar(280)", maxLength: 280, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastDirection = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastIncomingAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastOutgoingAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastInteractionAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsappconversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_whatsappconversations_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "whatsappmessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CompanyId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    WhatsAppConversationId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    IsInbound = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MessageType = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExternalMessageId = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GeneratedByAi = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeliveredAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReadAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsappmessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_whatsappmessages_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_whatsappmessages_whatsappconversations_WhatsAppConversationId",
                        column: x => x.WhatsAppConversationId,
                        principalTable: "whatsappconversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_whatsappconversations_CompanyId_ExternalPhone",
                table: "whatsappconversations",
                columns: new[] { "CompanyId", "ExternalPhone" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_whatsappconversations_CompanyId_LastInteractionAtUtc",
                table: "whatsappconversations",
                columns: new[] { "CompanyId", "LastInteractionAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_whatsappmessages_CompanyId_CreatedAtUtc",
                table: "whatsappmessages",
                columns: new[] { "CompanyId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_whatsappmessages_ExternalMessageId",
                table: "whatsappmessages",
                column: "ExternalMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_whatsappmessages_WhatsAppConversationId",
                table: "whatsappmessages",
                column: "WhatsAppConversationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "whatsappmessages");

            migrationBuilder.DropTable(
                name: "whatsappconversations");

            migrationBuilder.DropColumn(
                name: "EnableWhatsAppAssistant",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "IsWhatsAppConnected",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "WhatsAppAccountSecurityTokenCipherText",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "WhatsAppConnectedAtUtc",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "WhatsAppConnectedPhone",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "WhatsAppDisconnectedAtUtc",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "WhatsAppInstanceId",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "WhatsAppInstanceTokenCipherText",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "WhatsAppLastIncomingAtUtc",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "WhatsAppLastOutgoingAtUtc",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "WhatsAppWebhookSecretCipherText",
                table: "companies");
        }
    }
}
