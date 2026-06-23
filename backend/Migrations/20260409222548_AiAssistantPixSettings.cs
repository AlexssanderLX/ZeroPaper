using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class AiAssistantPixSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiAssistantPixKey",
                table: "companies",
                type: "varchar(180)",
                maxLength: 180,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AiAssistantPixMessage",
                table: "companies",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AiAssistantPixReceiverName",
                table: "companies",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiAssistantPixKey",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "AiAssistantPixMessage",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "AiAssistantPixReceiverName",
                table: "companies");
        }
    }
}
