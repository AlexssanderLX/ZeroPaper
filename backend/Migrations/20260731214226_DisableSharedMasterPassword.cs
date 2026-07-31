using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class DisableSharedMasterPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE companies
                SET AdminMasterPasswordHash = NULL,
                    AdminMasterPasswordCipherText = NULL,
                    AdminMasterPasswordRotatedAtUtc = NULL
                WHERE AdminMasterPasswordHash IS NOT NULL
                   OR AdminMasterPasswordCipherText IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Segredos compartilhados removidos nao podem ser recuperados com seguranca.
        }
    }
}
