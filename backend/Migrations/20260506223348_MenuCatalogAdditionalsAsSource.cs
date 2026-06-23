using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class MenuCatalogAdditionalsAsSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceMenuAdditionalCatalogOptionId",
                table: "menuitemadditionaloptions",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "SourceMenuAdditionalCatalogGroupId",
                table: "menuitemadditionalgroups",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<int>(
                name: "MaxAdditionalSelections",
                table: "menuadditionalcataloggroups",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE menuadditionalcataloggroups mcg
                JOIN menuadditionalcatalogoptions mco
                    ON mco.MenuAdditionalCatalogGroupId = mcg.Id
                    AND mco.CompanyId = mcg.CompanyId
                    AND mco.IsActive = 1
                JOIN menuitemadditionaloptions mio
                    ON mio.CompanyId = mcg.CompanyId
                    AND mio.Name = mco.Name
                    AND mio.Price = mco.Price
                    AND mio.IsActive = 1
                JOIN menuitemadditionalgroups mig
                    ON mig.Id = mio.MenuItemAdditionalGroupId
                    AND mig.CompanyId = mcg.CompanyId
                    AND mig.IsActive = 1
                SET mcg.MaxAdditionalSelections = mig.MaxAdditionalSelections
                WHERE mcg.MaxAdditionalSelections IS NULL
                    AND mig.MaxAdditionalSelections IS NOT NULL
                    AND (
                        LOWER(mig.Name) = LOWER(mcg.Name)
                        OR LOWER(mig.Name) = LOWER(mco.Name)
                    );
                """);

            migrationBuilder.Sql("""
                UPDATE menuitemadditionalgroups mig
                JOIN menuitemadditionaloptions mio
                    ON mio.MenuItemAdditionalGroupId = mig.Id
                    AND mio.CompanyId = mig.CompanyId
                    AND mio.IsActive = 1
                JOIN menuadditionalcatalogoptions mco
                    ON mco.CompanyId = mig.CompanyId
                    AND mco.Name = mio.Name
                    AND mco.Price = mio.Price
                    AND mco.IsActive = 1
                JOIN menuadditionalcataloggroups mcg
                    ON mcg.Id = mco.MenuAdditionalCatalogGroupId
                    AND mcg.CompanyId = mig.CompanyId
                    AND mcg.IsActive = 1
                SET mig.SourceMenuAdditionalCatalogGroupId = mcg.Id,
                    mig.MaxAdditionalSelections = COALESCE(mig.MaxAdditionalSelections, mcg.MaxAdditionalSelections),
                    mio.SourceMenuAdditionalCatalogOptionId = mco.Id
                WHERE mig.SourceMenuAdditionalCatalogGroupId IS NULL
                    AND (
                        LOWER(mig.Name) = LOWER(mcg.Name)
                        OR LOWER(mig.Name) = LOWER(mco.Name)
                    );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_menuitemadditionaloptions_SourceMenuAdditionalCatalogOptionId",
                table: "menuitemadditionaloptions",
                column: "SourceMenuAdditionalCatalogOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_menuitemadditionalgroups_SourceMenuAdditionalCatalogGroupId",
                table: "menuitemadditionalgroups",
                column: "SourceMenuAdditionalCatalogGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_menuitemadditionaloptions_SourceMenuAdditionalCatalogOptionId",
                table: "menuitemadditionaloptions");

            migrationBuilder.DropIndex(
                name: "IX_menuitemadditionalgroups_SourceMenuAdditionalCatalogGroupId",
                table: "menuitemadditionalgroups");

            migrationBuilder.DropColumn(
                name: "SourceMenuAdditionalCatalogOptionId",
                table: "menuitemadditionaloptions");

            migrationBuilder.DropColumn(
                name: "SourceMenuAdditionalCatalogGroupId",
                table: "menuitemadditionalgroups");

            migrationBuilder.DropColumn(
                name: "MaxAdditionalSelections",
                table: "menuadditionalcataloggroups");
        }
    }
}
