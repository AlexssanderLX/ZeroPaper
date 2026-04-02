using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroPaper.Migrations
{
    /// <inheritdoc />
    public partial class WaiterCallAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Tenants_TenantId",
                table: "Companies");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerOrders_Companies_CompanyId",
                table: "CustomerOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerOrders_DiningTables_DiningTableId",
                table: "CustomerOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerOrders_Tenants_TenantId",
                table: "CustomerOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_DiningTables_Companies_CompanyId",
                table: "DiningTables");

            migrationBuilder.DropForeignKey(
                name: "FK_DiningTables_QrCodeAccesses_QrCodeAccessId",
                table: "DiningTables");

            migrationBuilder.DropForeignKey(
                name: "FK_DiningTables_Tenants_TenantId",
                table: "DiningTables");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuCategories_Companies_CompanyId",
                table: "MenuCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuCategories_Tenants_TenantId",
                table: "MenuCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_Companies_CompanyId",
                table: "MenuItems");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_MenuCategories_MenuCategoryId",
                table: "MenuItems");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_Tenants_TenantId",
                table: "MenuItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_CustomerOrders_CustomerOrderId",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PasswordResetRequests_Users_AppUserId",
                table: "PasswordResetRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_QrCodeAccesses_Companies_CompanyId",
                table: "QrCodeAccesses");

            migrationBuilder.DropForeignKey(
                name: "FK_QrCodeAccesses_Tenants_TenantId",
                table: "QrCodeAccesses");

            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Companies_CompanyId",
                table: "Sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Tenants_TenantId",
                table: "Sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Users_AppUserId",
                table: "Sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_StockItems_Companies_CompanyId",
                table: "StockItems");

            migrationBuilder.DropForeignKey(
                name: "FK_StockItems_Tenants_TenantId",
                table: "StockItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Tenants_TenantId",
                table: "Subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Companies_CompanyId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tenants",
                table: "Tenants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Subscriptions",
                table: "Subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StockItems",
                table: "StockItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SignupCodes",
                table: "SignupCodes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Sessions",
                table: "Sessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QrCodeAccesses",
                table: "QrCodeAccesses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PasswordResetRequests",
                table: "PasswordResetRequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderItems",
                table: "OrderItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MenuItems",
                table: "MenuItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MenuCategories",
                table: "MenuCategories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DiningTables",
                table: "DiningTables");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CustomerOrders",
                table: "CustomerOrders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Companies",
                table: "Companies");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "users");

            migrationBuilder.RenameTable(
                name: "Tenants",
                newName: "tenants");

            migrationBuilder.RenameTable(
                name: "Subscriptions",
                newName: "subscriptions");

            migrationBuilder.RenameTable(
                name: "StockItems",
                newName: "stockitems");

            migrationBuilder.RenameTable(
                name: "SignupCodes",
                newName: "signupcodes");

            migrationBuilder.RenameTable(
                name: "Sessions",
                newName: "sessions");

            migrationBuilder.RenameTable(
                name: "QrCodeAccesses",
                newName: "qrcodeaccesses");

            migrationBuilder.RenameTable(
                name: "PasswordResetRequests",
                newName: "passwordresetrequests");

            migrationBuilder.RenameTable(
                name: "OrderItems",
                newName: "orderitems");

            migrationBuilder.RenameTable(
                name: "MenuItems",
                newName: "menuitems");

            migrationBuilder.RenameTable(
                name: "MenuCategories",
                newName: "menucategories");

            migrationBuilder.RenameTable(
                name: "DiningTables",
                newName: "diningtables");

            migrationBuilder.RenameTable(
                name: "CustomerOrders",
                newName: "customerorders");

            migrationBuilder.RenameTable(
                name: "Companies",
                newName: "companies");

            migrationBuilder.RenameIndex(
                name: "IX_Users_TenantId_Email",
                table: "users",
                newName: "IX_users_TenantId_Email");

            migrationBuilder.RenameIndex(
                name: "IX_Users_CompanyId",
                table: "users",
                newName: "IX_users_CompanyId");

            migrationBuilder.RenameIndex(
                name: "IX_Tenants_Identifier",
                table: "tenants",
                newName: "IX_tenants_Identifier");

            migrationBuilder.RenameIndex(
                name: "IX_Subscriptions_TenantId",
                table: "subscriptions",
                newName: "IX_subscriptions_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_StockItems_TenantId",
                table: "stockitems",
                newName: "IX_stockitems_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_StockItems_CompanyId_Name",
                table: "stockitems",
                newName: "IX_stockitems_CompanyId_Name");

            migrationBuilder.RenameIndex(
                name: "IX_SignupCodes_CodeHash",
                table: "signupcodes",
                newName: "IX_signupcodes_CodeHash");

            migrationBuilder.RenameIndex(
                name: "IX_Sessions_TokenHash",
                table: "sessions",
                newName: "IX_sessions_TokenHash");

            migrationBuilder.RenameIndex(
                name: "IX_Sessions_TenantId",
                table: "sessions",
                newName: "IX_sessions_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Sessions_CompanyId",
                table: "sessions",
                newName: "IX_sessions_CompanyId");

            migrationBuilder.RenameIndex(
                name: "IX_Sessions_AppUserId",
                table: "sessions",
                newName: "IX_sessions_AppUserId");

            migrationBuilder.RenameIndex(
                name: "IX_QrCodeAccesses_TenantId",
                table: "qrcodeaccesses",
                newName: "IX_qrcodeaccesses_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_QrCodeAccesses_PublicCode",
                table: "qrcodeaccesses",
                newName: "IX_qrcodeaccesses_PublicCode");

            migrationBuilder.RenameIndex(
                name: "IX_QrCodeAccesses_CompanyId",
                table: "qrcodeaccesses",
                newName: "IX_qrcodeaccesses_CompanyId");

            migrationBuilder.RenameIndex(
                name: "IX_PasswordResetRequests_TokenHash",
                table: "passwordresetrequests",
                newName: "IX_passwordresetrequests_TokenHash");

            migrationBuilder.RenameIndex(
                name: "IX_PasswordResetRequests_AppUserId_IsActive",
                table: "passwordresetrequests",
                newName: "IX_passwordresetrequests_AppUserId_IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_CustomerOrderId",
                table: "orderitems",
                newName: "IX_orderitems_CustomerOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_MenuItems_TenantId",
                table: "menuitems",
                newName: "IX_menuitems_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_MenuItems_MenuCategoryId",
                table: "menuitems",
                newName: "IX_menuitems_MenuCategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_MenuItems_CompanyId_MenuCategoryId_Name",
                table: "menuitems",
                newName: "IX_menuitems_CompanyId_MenuCategoryId_Name");

            migrationBuilder.RenameIndex(
                name: "IX_MenuCategories_TenantId",
                table: "menucategories",
                newName: "IX_menucategories_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_MenuCategories_CompanyId_Name",
                table: "menucategories",
                newName: "IX_menucategories_CompanyId_Name");

            migrationBuilder.RenameIndex(
                name: "IX_DiningTables_TenantId",
                table: "diningtables",
                newName: "IX_diningtables_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_DiningTables_QrCodeAccessId",
                table: "diningtables",
                newName: "IX_diningtables_QrCodeAccessId");

            migrationBuilder.RenameIndex(
                name: "IX_DiningTables_CompanyId_InternalCode",
                table: "diningtables",
                newName: "IX_diningtables_CompanyId_InternalCode");

            migrationBuilder.RenameIndex(
                name: "IX_CustomerOrders_TenantId",
                table: "customerorders",
                newName: "IX_customerorders_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_CustomerOrders_DiningTableId",
                table: "customerorders",
                newName: "IX_customerorders_DiningTableId");

            migrationBuilder.RenameIndex(
                name: "IX_CustomerOrders_CompanyId_Number",
                table: "customerorders",
                newName: "IX_customerorders_CompanyId_Number");

            migrationBuilder.RenameIndex(
                name: "IX_Companies_TenantId_AccessSlug",
                table: "companies",
                newName: "IX_companies_TenantId_AccessSlug");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                table: "users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tenants",
                table: "tenants",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_subscriptions",
                table: "subscriptions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_stockitems",
                table: "stockitems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_signupcodes",
                table: "signupcodes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sessions",
                table: "sessions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_qrcodeaccesses",
                table: "qrcodeaccesses",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_passwordresetrequests",
                table: "passwordresetrequests",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_orderitems",
                table: "orderitems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_menuitems",
                table: "menuitems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_menucategories",
                table: "menucategories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_diningtables",
                table: "diningtables",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_customerorders",
                table: "customerorders",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_companies",
                table: "companies",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "waitercalls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CompanyId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DiningTableId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_waitercalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_waitercalls_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_waitercalls_diningtables_DiningTableId",
                        column: x => x.DiningTableId,
                        principalTable: "diningtables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_waitercalls_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_waitercalls_CompanyId_DiningTableId_ResolvedAtUtc",
                table: "waitercalls",
                columns: new[] { "CompanyId", "DiningTableId", "ResolvedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_waitercalls_DiningTableId",
                table: "waitercalls",
                column: "DiningTableId");

            migrationBuilder.CreateIndex(
                name: "IX_waitercalls_TenantId",
                table: "waitercalls",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_companies_tenants_TenantId",
                table: "companies",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_customerorders_companies_CompanyId",
                table: "customerorders",
                column: "CompanyId",
                principalTable: "companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_customerorders_diningtables_DiningTableId",
                table: "customerorders",
                column: "DiningTableId",
                principalTable: "diningtables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_customerorders_tenants_TenantId",
                table: "customerorders",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_diningtables_companies_CompanyId",
                table: "diningtables",
                column: "CompanyId",
                principalTable: "companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_diningtables_qrcodeaccesses_QrCodeAccessId",
                table: "diningtables",
                column: "QrCodeAccessId",
                principalTable: "qrcodeaccesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_diningtables_tenants_TenantId",
                table: "diningtables",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_menucategories_companies_CompanyId",
                table: "menucategories",
                column: "CompanyId",
                principalTable: "companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_menucategories_tenants_TenantId",
                table: "menucategories",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_menuitems_companies_CompanyId",
                table: "menuitems",
                column: "CompanyId",
                principalTable: "companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_menuitems_menucategories_MenuCategoryId",
                table: "menuitems",
                column: "MenuCategoryId",
                principalTable: "menucategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_menuitems_tenants_TenantId",
                table: "menuitems",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_orderitems_customerorders_CustomerOrderId",
                table: "orderitems",
                column: "CustomerOrderId",
                principalTable: "customerorders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_passwordresetrequests_users_AppUserId",
                table: "passwordresetrequests",
                column: "AppUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_qrcodeaccesses_companies_CompanyId",
                table: "qrcodeaccesses",
                column: "CompanyId",
                principalTable: "companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_qrcodeaccesses_tenants_TenantId",
                table: "qrcodeaccesses",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_sessions_companies_CompanyId",
                table: "sessions",
                column: "CompanyId",
                principalTable: "companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_sessions_tenants_TenantId",
                table: "sessions",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_sessions_users_AppUserId",
                table: "sessions",
                column: "AppUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stockitems_companies_CompanyId",
                table: "stockitems",
                column: "CompanyId",
                principalTable: "companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stockitems_tenants_TenantId",
                table: "stockitems",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_subscriptions_tenants_TenantId",
                table: "subscriptions",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_users_companies_CompanyId",
                table: "users",
                column: "CompanyId",
                principalTable: "companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_users_tenants_TenantId",
                table: "users",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_companies_tenants_TenantId",
                table: "companies");

            migrationBuilder.DropForeignKey(
                name: "FK_customerorders_companies_CompanyId",
                table: "customerorders");

            migrationBuilder.DropForeignKey(
                name: "FK_customerorders_diningtables_DiningTableId",
                table: "customerorders");

            migrationBuilder.DropForeignKey(
                name: "FK_customerorders_tenants_TenantId",
                table: "customerorders");

            migrationBuilder.DropForeignKey(
                name: "FK_diningtables_companies_CompanyId",
                table: "diningtables");

            migrationBuilder.DropForeignKey(
                name: "FK_diningtables_qrcodeaccesses_QrCodeAccessId",
                table: "diningtables");

            migrationBuilder.DropForeignKey(
                name: "FK_diningtables_tenants_TenantId",
                table: "diningtables");

            migrationBuilder.DropForeignKey(
                name: "FK_menucategories_companies_CompanyId",
                table: "menucategories");

            migrationBuilder.DropForeignKey(
                name: "FK_menucategories_tenants_TenantId",
                table: "menucategories");

            migrationBuilder.DropForeignKey(
                name: "FK_menuitems_companies_CompanyId",
                table: "menuitems");

            migrationBuilder.DropForeignKey(
                name: "FK_menuitems_menucategories_MenuCategoryId",
                table: "menuitems");

            migrationBuilder.DropForeignKey(
                name: "FK_menuitems_tenants_TenantId",
                table: "menuitems");

            migrationBuilder.DropForeignKey(
                name: "FK_orderitems_customerorders_CustomerOrderId",
                table: "orderitems");

            migrationBuilder.DropForeignKey(
                name: "FK_passwordresetrequests_users_AppUserId",
                table: "passwordresetrequests");

            migrationBuilder.DropForeignKey(
                name: "FK_qrcodeaccesses_companies_CompanyId",
                table: "qrcodeaccesses");

            migrationBuilder.DropForeignKey(
                name: "FK_qrcodeaccesses_tenants_TenantId",
                table: "qrcodeaccesses");

            migrationBuilder.DropForeignKey(
                name: "FK_sessions_companies_CompanyId",
                table: "sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_sessions_tenants_TenantId",
                table: "sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_sessions_users_AppUserId",
                table: "sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_stockitems_companies_CompanyId",
                table: "stockitems");

            migrationBuilder.DropForeignKey(
                name: "FK_stockitems_tenants_TenantId",
                table: "stockitems");

            migrationBuilder.DropForeignKey(
                name: "FK_subscriptions_tenants_TenantId",
                table: "subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_users_companies_CompanyId",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "FK_users_tenants_TenantId",
                table: "users");

            migrationBuilder.DropTable(
                name: "waitercalls");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tenants",
                table: "tenants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_subscriptions",
                table: "subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_stockitems",
                table: "stockitems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_signupcodes",
                table: "signupcodes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sessions",
                table: "sessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_qrcodeaccesses",
                table: "qrcodeaccesses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_passwordresetrequests",
                table: "passwordresetrequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_orderitems",
                table: "orderitems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_menuitems",
                table: "menuitems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_menucategories",
                table: "menucategories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_diningtables",
                table: "diningtables");

            migrationBuilder.DropPrimaryKey(
                name: "PK_customerorders",
                table: "customerorders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_companies",
                table: "companies");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "tenants",
                newName: "Tenants");

            migrationBuilder.RenameTable(
                name: "subscriptions",
                newName: "Subscriptions");

            migrationBuilder.RenameTable(
                name: "stockitems",
                newName: "StockItems");

            migrationBuilder.RenameTable(
                name: "signupcodes",
                newName: "SignupCodes");

            migrationBuilder.RenameTable(
                name: "sessions",
                newName: "Sessions");

            migrationBuilder.RenameTable(
                name: "qrcodeaccesses",
                newName: "QrCodeAccesses");

            migrationBuilder.RenameTable(
                name: "passwordresetrequests",
                newName: "PasswordResetRequests");

            migrationBuilder.RenameTable(
                name: "orderitems",
                newName: "OrderItems");

            migrationBuilder.RenameTable(
                name: "menuitems",
                newName: "MenuItems");

            migrationBuilder.RenameTable(
                name: "menucategories",
                newName: "MenuCategories");

            migrationBuilder.RenameTable(
                name: "diningtables",
                newName: "DiningTables");

            migrationBuilder.RenameTable(
                name: "customerorders",
                newName: "CustomerOrders");

            migrationBuilder.RenameTable(
                name: "companies",
                newName: "Companies");

            migrationBuilder.RenameIndex(
                name: "IX_users_TenantId_Email",
                table: "Users",
                newName: "IX_Users_TenantId_Email");

            migrationBuilder.RenameIndex(
                name: "IX_users_CompanyId",
                table: "Users",
                newName: "IX_Users_CompanyId");

            migrationBuilder.RenameIndex(
                name: "IX_tenants_Identifier",
                table: "Tenants",
                newName: "IX_Tenants_Identifier");

            migrationBuilder.RenameIndex(
                name: "IX_subscriptions_TenantId",
                table: "Subscriptions",
                newName: "IX_Subscriptions_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_stockitems_TenantId",
                table: "StockItems",
                newName: "IX_StockItems_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_stockitems_CompanyId_Name",
                table: "StockItems",
                newName: "IX_StockItems_CompanyId_Name");

            migrationBuilder.RenameIndex(
                name: "IX_signupcodes_CodeHash",
                table: "SignupCodes",
                newName: "IX_SignupCodes_CodeHash");

            migrationBuilder.RenameIndex(
                name: "IX_sessions_TokenHash",
                table: "Sessions",
                newName: "IX_Sessions_TokenHash");

            migrationBuilder.RenameIndex(
                name: "IX_sessions_TenantId",
                table: "Sessions",
                newName: "IX_Sessions_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_sessions_CompanyId",
                table: "Sessions",
                newName: "IX_Sessions_CompanyId");

            migrationBuilder.RenameIndex(
                name: "IX_sessions_AppUserId",
                table: "Sessions",
                newName: "IX_Sessions_AppUserId");

            migrationBuilder.RenameIndex(
                name: "IX_qrcodeaccesses_TenantId",
                table: "QrCodeAccesses",
                newName: "IX_QrCodeAccesses_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_qrcodeaccesses_PublicCode",
                table: "QrCodeAccesses",
                newName: "IX_QrCodeAccesses_PublicCode");

            migrationBuilder.RenameIndex(
                name: "IX_qrcodeaccesses_CompanyId",
                table: "QrCodeAccesses",
                newName: "IX_QrCodeAccesses_CompanyId");

            migrationBuilder.RenameIndex(
                name: "IX_passwordresetrequests_TokenHash",
                table: "PasswordResetRequests",
                newName: "IX_PasswordResetRequests_TokenHash");

            migrationBuilder.RenameIndex(
                name: "IX_passwordresetrequests_AppUserId_IsActive",
                table: "PasswordResetRequests",
                newName: "IX_PasswordResetRequests_AppUserId_IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_orderitems_CustomerOrderId",
                table: "OrderItems",
                newName: "IX_OrderItems_CustomerOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_menuitems_TenantId",
                table: "MenuItems",
                newName: "IX_MenuItems_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_menuitems_MenuCategoryId",
                table: "MenuItems",
                newName: "IX_MenuItems_MenuCategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_menuitems_CompanyId_MenuCategoryId_Name",
                table: "MenuItems",
                newName: "IX_MenuItems_CompanyId_MenuCategoryId_Name");

            migrationBuilder.RenameIndex(
                name: "IX_menucategories_TenantId",
                table: "MenuCategories",
                newName: "IX_MenuCategories_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_menucategories_CompanyId_Name",
                table: "MenuCategories",
                newName: "IX_MenuCategories_CompanyId_Name");

            migrationBuilder.RenameIndex(
                name: "IX_diningtables_TenantId",
                table: "DiningTables",
                newName: "IX_DiningTables_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_diningtables_QrCodeAccessId",
                table: "DiningTables",
                newName: "IX_DiningTables_QrCodeAccessId");

            migrationBuilder.RenameIndex(
                name: "IX_diningtables_CompanyId_InternalCode",
                table: "DiningTables",
                newName: "IX_DiningTables_CompanyId_InternalCode");

            migrationBuilder.RenameIndex(
                name: "IX_customerorders_TenantId",
                table: "CustomerOrders",
                newName: "IX_CustomerOrders_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_customerorders_DiningTableId",
                table: "CustomerOrders",
                newName: "IX_CustomerOrders_DiningTableId");

            migrationBuilder.RenameIndex(
                name: "IX_customerorders_CompanyId_Number",
                table: "CustomerOrders",
                newName: "IX_CustomerOrders_CompanyId_Number");

            migrationBuilder.RenameIndex(
                name: "IX_companies_TenantId_AccessSlug",
                table: "Companies",
                newName: "IX_Companies_TenantId_AccessSlug");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tenants",
                table: "Tenants",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Subscriptions",
                table: "Subscriptions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StockItems",
                table: "StockItems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SignupCodes",
                table: "SignupCodes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Sessions",
                table: "Sessions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QrCodeAccesses",
                table: "QrCodeAccesses",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PasswordResetRequests",
                table: "PasswordResetRequests",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderItems",
                table: "OrderItems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MenuItems",
                table: "MenuItems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MenuCategories",
                table: "MenuCategories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DiningTables",
                table: "DiningTables",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CustomerOrders",
                table: "CustomerOrders",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Companies",
                table: "Companies",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Tenants_TenantId",
                table: "Companies",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerOrders_Companies_CompanyId",
                table: "CustomerOrders",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerOrders_DiningTables_DiningTableId",
                table: "CustomerOrders",
                column: "DiningTableId",
                principalTable: "DiningTables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerOrders_Tenants_TenantId",
                table: "CustomerOrders",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DiningTables_Companies_CompanyId",
                table: "DiningTables",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DiningTables_QrCodeAccesses_QrCodeAccessId",
                table: "DiningTables",
                column: "QrCodeAccessId",
                principalTable: "QrCodeAccesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DiningTables_Tenants_TenantId",
                table: "DiningTables",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuCategories_Companies_CompanyId",
                table: "MenuCategories",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuCategories_Tenants_TenantId",
                table: "MenuCategories",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_Companies_CompanyId",
                table: "MenuItems",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_MenuCategories_MenuCategoryId",
                table: "MenuItems",
                column: "MenuCategoryId",
                principalTable: "MenuCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_Tenants_TenantId",
                table: "MenuItems",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_CustomerOrders_CustomerOrderId",
                table: "OrderItems",
                column: "CustomerOrderId",
                principalTable: "CustomerOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PasswordResetRequests_Users_AppUserId",
                table: "PasswordResetRequests",
                column: "AppUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QrCodeAccesses_Companies_CompanyId",
                table: "QrCodeAccesses",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QrCodeAccesses_Tenants_TenantId",
                table: "QrCodeAccesses",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Companies_CompanyId",
                table: "Sessions",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Tenants_TenantId",
                table: "Sessions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Users_AppUserId",
                table: "Sessions",
                column: "AppUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockItems_Companies_CompanyId",
                table: "StockItems",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockItems_Tenants_TenantId",
                table: "StockItems",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Tenants_TenantId",
                table: "Subscriptions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Companies_CompanyId",
                table: "Users",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
