using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class AiAssistantSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiAssistantFallbackMessage",
                table: "companies",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "Agora preciso encaminhar voce para a equipe da unidade revisar isso com seguranca.")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AiAssistantGreetingMessage",
                table: "companies",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "Ola! Posso ajudar com duvidas do atendimento e encaminhar voce para o pedido oficial da unidade.")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "AiAssistantMaxOutputTokens",
                table: "companies",
                type: "int",
                nullable: false,
                defaultValue: 220);

            migrationBuilder.AddColumn<string>(
                name: "AiAssistantModel",
                table: "companies",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "gpt-5.4-mini")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AiAssistantRedirectMessage",
                table: "companies",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "Para finalizar com seguranca, use o link oficial do ZeroPaper da unidade e conclua os dados no sistema.")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AiAssistantSystemPrompt",
                table: "companies",
                type: "varchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "Atue como atendente digital do ZeroPaper em portugues do Brasil. Seja objetivo, cordial e claro. Nao invente itens, valores, disponibilidade ou prazos. Oriente o cliente para o fluxo oficial do sistema sempre que for necessario fechar pedido, confirmar endereco, nome ou pagamento. Se houver duvida fora desse escopo, admita o limite e use a mensagem de fallback configurada pela unidade.")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "EnableAiAssistant",
                table: "companies",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiAssistantFallbackMessage",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "AiAssistantGreetingMessage",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "AiAssistantMaxOutputTokens",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "AiAssistantModel",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "AiAssistantRedirectMessage",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "AiAssistantSystemPrompt",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "EnableAiAssistant",
                table: "companies");
        }
    }
}
