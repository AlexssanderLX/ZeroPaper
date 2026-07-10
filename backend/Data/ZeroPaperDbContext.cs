using Microsoft.EntityFrameworkCore;
using ZeroPaper.Domain.Entities;

namespace ZeroPaper.Data;

public class ZeroPaperDbContext : DbContext
{
    public ZeroPaperDbContext(DbContextOptions<ZeroPaperDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AppSession> Sessions => Set<AppSession>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<QrCodeAccess> QrCodeAccesses => Set<QrCodeAccess>();
    public DbSet<DiningTable> DiningTables => Set<DiningTable>();
    public DbSet<CustomerOrder> CustomerOrders => Set<CustomerOrder>();
    public DbSet<DeliveryCustomerProfile> DeliveryCustomerProfiles => Set<DeliveryCustomerProfile>();
    public DbSet<CustomerOrderHistory> CustomerOrderHistories => Set<CustomerOrderHistory>();
    public DbSet<CustomerOrderHistoryItem> CustomerOrderHistoryItems => Set<CustomerOrderHistoryItem>();
    public DbSet<DeliveryDistanceCache> DeliveryDistanceCaches => Set<DeliveryDistanceCache>();
    public DbSet<DeletedOrderRecord> DeletedOrderRecords => Set<DeletedOrderRecord>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemAdditionalSelection> OrderItemAdditionalSelections => Set<OrderItemAdditionalSelection>();
    public DbSet<CustomerOrderPayment> CustomerOrderPayments => Set<CustomerOrderPayment>();
    public DbSet<WaiterCall> WaiterCalls => Set<WaiterCall>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MenuAdditionalCatalogGroup> MenuAdditionalCatalogGroups => Set<MenuAdditionalCatalogGroup>();
    public DbSet<MenuAdditionalCatalogOption> MenuAdditionalCatalogOptions => Set<MenuAdditionalCatalogOption>();
    public DbSet<MenuItemAdditionalGroup> MenuItemAdditionalGroups => Set<MenuItemAdditionalGroup>();
    public DbSet<MenuItemAdditionalOption> MenuItemAdditionalOptions => Set<MenuItemAdditionalOption>();
    public DbSet<SignupCode> SignupCodes => Set<SignupCode>();
    public DbSet<PasswordResetRequest> PasswordResetRequests => Set<PasswordResetRequest>();
    public DbSet<AiAssistantInteraction> AiAssistantInteractions => Set<AiAssistantInteraction>();
    public DbSet<WhatsAppConversation> WhatsAppConversations => Set<WhatsAppConversation>();
    public DbSet<WhatsAppMessage> WhatsAppMessages => Set<WhatsAppMessage>();
    public DbSet<ManualPixConfirmation> ManualPixConfirmations => Set<ManualPixConfirmation>();
    public DbSet<DailySalesSnapshot> DailySalesSnapshots => Set<DailySalesSnapshot>();
    public DbSet<PrintAgent> PrintAgents => Set<PrintAgent>();
    public DbSet<PrintJob> PrintJobs => Set<PrintJob>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<SalesAgent> SalesAgents => Set<SalesAgent>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasCharSet("utf8mb4");

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Identifier).HasMaxLength(80).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => x.Identifier).IsUnique();
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("companies");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.LegalName).HasMaxLength(180).IsRequired();
            entity.Property(x => x.TradeName).HasMaxLength(180).IsRequired();
            entity.Property(x => x.LogoUrl).HasColumnType("text");
            entity.Property(x => x.AccessSlug).HasMaxLength(80).IsRequired();
            entity.Property(x => x.DocumentNumber).HasMaxLength(30);
            entity.Property(x => x.ContactEmail).HasMaxLength(180);
            entity.Property(x => x.ContactPhone).HasMaxLength(30);
            entity.Property(x => x.LastOrderNumber).IsRequired();
            entity.Property(x => x.EnableOrderAlerts).IsRequired();
            entity.Property(x => x.EnableWaiterCallAlerts).IsRequired();
            entity.Property(x => x.AlertSoundUrl).HasMaxLength(500);
            entity.Property(x => x.AlertVolumePercent).IsRequired();
            entity.Property(x => x.AlertPlaybackSeconds).IsRequired();
            entity.Property(x => x.EnableAutomaticPrinting).IsRequired();
            entity.Property(x => x.PrintPaperProfile)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.PrintOrdersPerPage).IsRequired();
            entity.Property(x => x.PrintAgentKeyHash).HasMaxLength(128);
            entity.Property(x => x.PrintAgentName).HasMaxLength(120);
            entity.Property(x => x.PrintAgentPrinterName).HasMaxLength(180);
            entity.Property(x => x.EnableAiAssistant).IsRequired();
            entity.Property(x => x.AiAssistantModel).HasMaxLength(80).IsRequired();
            entity.Property(x => x.AiAssistantSystemPrompt).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.AiAssistantGreetingMessage).HasMaxLength(500).IsRequired();
            entity.Property(x => x.AiAssistantRedirectMessage).HasMaxLength(500).IsRequired();
            entity.Property(x => x.AiAssistantFallbackMessage).HasMaxLength(500).IsRequired();
            entity.Property(x => x.AiAssistantOrderingLink).HasMaxLength(500);
            entity.Property(x => x.AiAssistantPixReceiverName).HasMaxLength(120);
            entity.Property(x => x.AiAssistantPixKey).HasMaxLength(180);
            entity.Property(x => x.AiAssistantPixMessage).HasMaxLength(500);
            entity.Property(x => x.AiAssistantServiceDays).HasMaxLength(20);
            entity.Property(x => x.AiAssistantServiceStartTime).HasMaxLength(5);
            entity.Property(x => x.AiAssistantServiceEndTime).HasMaxLength(5);
            entity.Property(x => x.AiAssistantMaxOutputTokens).IsRequired();
            entity.Property(x => x.EnableWhatsAppAssistant).IsRequired();
            entity.Property(x => x.WhatsAppInstanceId).HasMaxLength(80);
            entity.Property(x => x.WhatsAppInstanceTokenCipherText).HasMaxLength(2000);
            entity.Property(x => x.WhatsAppAccountSecurityTokenCipherText).HasMaxLength(2000);
            entity.Property(x => x.WhatsAppWebhookSecretCipherText).HasMaxLength(2000);
            entity.Property(x => x.IsWhatsAppConnected).IsRequired();
            entity.Property(x => x.WhatsAppConnectedPhone).HasMaxLength(40);
            entity.Property(x => x.EnableDeliveryFreight).IsRequired();
            entity.Property(x => x.DeliveryOriginPostalCode).HasMaxLength(8);
            entity.Property(x => x.DeliveryFreightPricePerKm).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.DeliveryFreightBaseFee).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.DeliveryFreightBaseDistanceKm).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.PickupEstimatedMinutes);
            entity.Property(x => x.DeliveryEstimatedMinutes);
            entity.Property(x => x.AdminMasterPasswordHash).HasMaxLength(255);
            entity.Property(x => x.AdminMasterPasswordCipherText).HasMaxLength(1000);
            entity.Property(x => x.MercadoPagoUserId).HasMaxLength(40);
            entity.Property(x => x.MercadoPagoAccessTokenCipherText).HasColumnType("longtext");
            entity.Property(x => x.MercadoPagoRefreshTokenCipherText).HasColumnType("longtext");
            entity.Property(x => x.MercadoPagoPublicKey).HasMaxLength(200);
            entity.Property(x => x.MercadoPagoLiveMode).IsRequired();
            entity.Property(x => x.IsMercadoPagoConnected).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => new { x.TenantId, x.AccessSlug }).IsUnique();

            entity.HasOne(x => x.Tenant)
                .WithMany(x => x.Companies)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.TimeZoneId)
            .HasMaxLength(100)
            .HasDefaultValue("America/Sao_Paulo")
            .IsRequired();
        });

        modelBuilder.Entity<DailySalesSnapshot>(entity =>
        {
            entity.ToTable("dailysalessnapshots");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.ReferenceDate)
                .HasColumnType("date")
                .IsRequired();

            entity.Property(x => x.TotalSalesAmount)
                .HasPrecision(12, 2)
                .IsRequired();

            entity.Property(x => x.PaidAmount)
                .HasPrecision(12, 2)
                .IsRequired();

            entity.Property(x => x.PendingAmount)
                .HasPrecision(12, 2)
                .IsRequired();

            entity.Property(x => x.CancelledAmount)
                .HasPrecision(12, 2)
                .IsRequired();

            entity.Property(x => x.DiscountAmount)
                .HasPrecision(12, 2)
                .IsRequired();

            entity.Property(x => x.SurchargeAmount)
                .HasPrecision(12, 2)
                .IsRequired();

            entity.Property(x => x.DeliveryFreightAmount)
                .HasPrecision(12, 2)
                .IsRequired();

            entity.Property(x => x.AverageTicket)
                .HasPrecision(12, 2)
                .IsRequired();

            entity.Property(x => x.GeneratedAtUtc)
                .IsRequired();

            entity.Property(x => x.DetailExpiresAtUtc)
                .IsRequired();

            entity.Property(x => x.HasDetailedData)
                .IsRequired();

            entity.HasIndex(x => new { x.CompanyId, x.ReferenceDate })
                .IsUnique();

            entity.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PrintAgent>(entity =>
        {
            entity.ToTable("printagents");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120);
            entity.Property(x => x.PrinterName).HasMaxLength(180);
            entity.Property(x => x.AppVersion).HasMaxLength(60);
            entity.Property(x => x.LastError).HasMaxLength(500);
            entity.Property(x => x.TokenRotatedAtUtc).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.IsActive });
            entity.HasIndex(x => new { x.CompanyId, x.LastSeenAtUtc });

            entity.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PrintJob>(entity =>
        {
            entity.ToTable("printjobs");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Kind)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(600);
            entity.Property(x => x.AgentName).HasMaxLength(120);
            entity.Property(x => x.PrinterName).HasMaxLength(180);
            entity.Property(x => x.LastError).HasMaxLength(500);
            entity.Property(x => x.QueuedAtUtc).IsRequired();
            entity.Property(x => x.Attempts).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => new { x.CompanyId, x.Status, x.QueuedAtUtc });
            entity.HasIndex(x => new { x.CompanyId, x.SourceOrderId });

            entity.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.ToTable("coupons");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(240);
            entity.Property(x => x.DiscountType)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.DiscountValue).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.MinimumOrderAmount).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.UsageCount).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.IsActive });

            entity.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(180).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(x => x.ShortcutAccessTokenHash).HasMaxLength(128);
            entity.Property(x => x.Role)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            entity.HasIndex(x => x.ShortcutAccessTokenHash).IsUnique();

            entity.HasOne(x => x.Tenant)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Company)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppSession>(entity =>
        {
            entity.ToTable("sessions");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.ExpiresAtUtc).IsRequired();

            entity.HasIndex(x => x.TokenHash).IsUnique();

            entity.HasOne(x => x.Tenant)
                .WithMany(x => x.Sessions)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Company)
                .WithMany(x => x.Sessions)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AppUser)
                .WithMany(x => x.Sessions)
                .HasForeignKey(x => x.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.ToTable("subscriptions");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.PlanName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.MonthlyPrice).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.IncludesMenuModule).IsRequired();
            entity.Property(x => x.IncludesTablesModule).IsRequired();
            entity.Property(x => x.IncludesKitchenModule).IsRequired();
            entity.Property(x => x.IncludesCashModule).IsRequired();
            entity.Property(x => x.IncludesStockModule).IsRequired();
            entity.Property(x => x.IncludesDeliveryModule).IsRequired();
            entity.Property(x => x.IncludesPrintingModule).IsRequired();
            entity.Property(x => x.IncludesWaiterCallModule).IsRequired();
            entity.Property(x => x.IncludesAiAssistantModule).IsRequired();
            entity.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasOne(x => x.Tenant)
                .WithMany(x => x.Subscriptions)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<QrCodeAccess>(entity =>
        {
            entity.ToTable("qrcodeaccesses");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Label).HasMaxLength(120).IsRequired();
            entity.Property(x => x.PublicCode).HasMaxLength(64).IsRequired();
            entity.Property(x => x.AccessPath).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => x.PublicCode).IsUnique();

            entity.HasOne(x => x.Tenant)
                .WithMany(x => x.QrCodeAccesses)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Company)
                .WithMany(x => x.QrCodeAccesses)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DiningTable>(entity =>
        {
            entity.ToTable("diningtables");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.InternalCode).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ComandaLabel).HasMaxLength(40);
            entity.Property(x => x.IsDeliveryChannel).IsRequired();
            entity.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.AlertSoundUrl).HasMaxLength(500);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => new { x.CompanyId, x.InternalCode }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.IsDeliveryChannel });
            entity.HasIndex(x => x.QrCodeAccessId).IsUnique();

            entity.HasOne(x => x.Tenant)
                .WithMany(x => x.Tables)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Company)
                .WithMany(x => x.Tables)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.QrCodeAccess)
                .WithOne(x => x.DiningTable)
                .HasForeignKey<DiningTable>(x => x.QrCodeAccessId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CustomerOrder>(entity =>
        {
            entity.ToTable("customerorders");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.CustomerName).HasMaxLength(120);
            entity.Property(x => x.Notes).HasMaxLength(600);
            entity.Property(x => x.DeliveryPhone).HasMaxLength(40);
            entity.Property(x => x.DeliveryAddress).HasMaxLength(220);
            entity.Property(x => x.DeliveryNumber).HasMaxLength(30);
            entity.Property(x => x.DeliveryComplement).HasMaxLength(160);
            entity.Property(x => x.DeliveryPostalCode).HasMaxLength(8);
            entity.Property(x => x.DeliveryFreightAmount).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.DeliveryDistanceKm).HasPrecision(10, 2);
            entity.Property(x => x.DeliveryFreightProvider).HasMaxLength(40);
            entity.Property(x => x.PublicEditCode).HasMaxLength(64);
            entity.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.PaymentMethod)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.RequestedPaymentMethod)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.PaymentStatus)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.PrintStatus)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.TotalAmount).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.IsEdited).IsRequired();
            entity.Property(x => x.DiscountAmount).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.SurchargeAmount).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.CouponCode).HasMaxLength(40);
            entity.Property(x => x.CouponDiscountAmount).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.PriceAdjustmentNote).HasMaxLength(240);
            entity.Property(x => x.PrintLastError).HasMaxLength(500);
            entity.Property(x => x.PrintAgentName).HasMaxLength(120);
            entity.Property(x => x.PrintPrinterName).HasMaxLength(180);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.SubmittedAtUtc).IsRequired();

            entity.HasIndex(x => new { x.CompanyId, x.Number }).IsUnique();
            entity.HasIndex(x => x.PublicEditCode).IsUnique();
            entity.HasIndex(x => x.CouponId);
            entity.HasIndex(x => new { x.CompanyId, x.SubmittedAtUtc, x.Status, x.PaymentStatus });
            entity.HasIndex(x => new { x.CompanyId, x.PrintStatus, x.SubmittedAtUtc });

            entity.HasOne(x => x.Tenant)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Company)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.DiningTable)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.DiningTableId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Coupon)
                .WithMany()
                .HasForeignKey(x => x.CouponId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Navigation(x => x.Items)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Navigation(x => x.Payments)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<CustomerOrderPayment>(entity =>
        {
            entity.ToTable("customerorderpayments");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Method)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.Amount).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => x.CustomerOrderId);

            entity.HasOne(x => x.CustomerOrder)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.CustomerOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeliveryDistanceCache>(entity =>
        {
            entity.ToTable("deliverydistancecaches");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.OriginPostalCode).HasMaxLength(8).IsRequired();
            entity.Property(x => x.DestinationPostalCode).HasMaxLength(8).IsRequired();
            entity.Property(x => x.Provider).HasMaxLength(40).IsRequired();
            entity.Property(x => x.DistanceKm).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.ExpiresAtUtc).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => new { x.CompanyId, x.Provider, x.OriginPostalCode, x.DestinationPostalCode }).IsUnique();
            entity.HasIndex(x => x.ExpiresAtUtc);

            entity.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeliveryCustomerProfile>(entity =>
        {
            entity.ToTable("deliverycustomerprofiles");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Phone).HasMaxLength(40).IsRequired();
            entity.Property(x => x.CustomerName).HasMaxLength(120);
            entity.Property(x => x.DeliveryAddress).HasMaxLength(220);
            entity.Property(x => x.DeliveryNumber).HasMaxLength(30);
            entity.Property(x => x.DeliveryNeighborhood).HasMaxLength(120);
            entity.Property(x => x.DeliveryComplement).HasMaxLength(160);
            entity.Property(x => x.DeliveryPostalCode).HasMaxLength(8);
            entity.Property(x => x.LastOrderAtUtc).IsRequired();
            entity.Property(x => x.PublicAccessCodeHash).HasMaxLength(128);
            entity.Property(x => x.PublicAccessCodeCipherText).HasMaxLength(1000);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => new { x.CompanyId, x.Phone }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.LastOrderAtUtc });
            entity.HasIndex(x => x.PublicAccessCodeHash).IsUnique();

            entity.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CustomerOrderHistory>(entity =>
        {
            entity.ToTable("customerorderhistories");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.TotalAmount).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => x.OrderId).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.CustomerProfileId, x.CreatedAtUtc });

            entity.HasOne(x => x.CustomerProfile)
                .WithMany()
                .HasForeignKey(x => x.CustomerProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CustomerOrderHistoryItem>(entity =>
        {
            entity.ToTable("customerorderhistoryitems");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ItemName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Quantity).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => x.CustomerOrderHistoryId);

            entity.HasOne(x => x.CustomerOrderHistory)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.CustomerOrderHistoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("orderitems");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.SourceMenuItemId);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.CategoryName).HasMaxLength(120);
            entity.Property(x => x.ImageUrl).HasMaxLength(500);
            entity.Property(x => x.Quantity).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.BaseUnitPrice).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.UnitPrice).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(300);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasOne(x => x.CustomerOrder)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.CustomerOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(x => x.AdditionalSelections)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<OrderItemAdditionalSelection>(entity =>
        {
            entity.ToTable("orderitemadditionalselections");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.SourceMenuItemAdditionalOptionId);
            entity.Property(x => x.GroupName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.OptionName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.UnitPrice).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasOne(x => x.OrderItem)
                .WithMany(x => x.AdditionalSelections)
                .HasForeignKey(x => x.OrderItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeletedOrderRecord>(entity =>
        {
            entity.ToTable("deletedorderrecords");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.TableName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.CustomerName).HasMaxLength(120);
            entity.Property(x => x.Notes).HasMaxLength(600);
            entity.Property(x => x.ItemsSummary).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.PaymentMethod)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.RequestedPaymentMethod)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.PaymentStatus)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.PrintStatus)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.TotalAmount).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.DeletedByUserName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.DeletionReason).HasMaxLength(160).IsRequired();
            entity.Property(x => x.SubmittedAtUtc).IsRequired();
            entity.Property(x => x.DeletedAtUtc).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => new { x.CompanyId, x.SubmittedAtUtc });
            entity.HasIndex(x => x.SourceOrderId);

            entity.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AiAssistantInteraction>(entity =>
        {
            entity.ToTable("aiassistantinteractions");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Source).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Model).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Succeeded).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => new { x.CompanyId, x.CreatedAtUtc });

            entity.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WhatsAppConversation>(entity =>
        {
            entity.ToTable("whatsappconversations");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ExternalPhone).HasMaxLength(40).IsRequired();
            entity.Property(x => x.CustomerName).HasMaxLength(120);
            entity.Property(x => x.LastMessagePreview).HasMaxLength(280);
            entity.Property(x => x.LastDirection).HasMaxLength(20).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => new { x.CompanyId, x.ExternalPhone }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.LastInteractionAtUtc });

            entity.HasOne(x => x.Company)
                .WithMany(x => x.WhatsAppConversations)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Navigation(x => x.Messages)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<WhatsAppMessage>(entity =>
        {
            entity.ToTable("whatsappmessages");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.MessageType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Content).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.ExternalMessageId).HasMaxLength(180);
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.Property(x => x.GeneratedByAi).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => x.ExternalMessageId);
            entity.HasIndex(x => new { x.CompanyId, x.CreatedAtUtc });

            entity.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Conversation)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.WhatsAppConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ManualPixConfirmation>(entity =>
        {
            entity.ToTable("manualpixconfirmations");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.CustomerName).HasMaxLength(120);
            entity.Property(x => x.CustomerPhone).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.PixKeyShown).HasMaxLength(180).IsRequired();
            entity.Property(x => x.ConfirmationPhrase).HasMaxLength(180).IsRequired();
            entity.Property(x => x.CustomerMessage).HasMaxLength(1000);
            entity.Property(x => x.ReceiptReference).HasMaxLength(500);
            entity.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.OwnerNote).HasMaxLength(1000);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => new { x.CompanyId, x.Status, x.CustomerConfirmedAtUtc });
            entity.HasIndex(x => new { x.CompanyId, x.CustomerPhone, x.CreatedAtUtc });
            entity.HasIndex(x => x.OrderId);

            entity.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Order)
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ReviewedByUser)
                .WithMany()
                .HasForeignKey(x => x.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WaiterCall>(entity =>
        {
            entity.ToTable("waitercalls");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.RequestedAtUtc).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => new { x.CompanyId, x.DiningTableId, x.ResolvedAtUtc });

            entity.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Company)
                .WithMany(x => x.WaiterCalls)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.DiningTable)
                .WithMany(x => x.WaiterCalls)
                .HasForeignKey(x => x.DiningTableId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockItem>(entity =>
        {
            entity.ToTable("stockitems");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Unit).HasMaxLength(30).IsRequired();
            entity.Property(x => x.CurrentQuantity).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.MinimumQuantity).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();

            entity.HasOne(x => x.Tenant)
                .WithMany(x => x.StockItems)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Company)
                .WithMany(x => x.StockItems)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MenuCategory>(entity =>
        {
            entity.ToTable("menucategories");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.ImageUrl).HasMaxLength(500);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();

            entity.HasOne(x => x.Tenant)
                .WithMany(x => x.MenuCategories)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Company)
                .WithMany(x => x.MenuCategories)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.ToTable("menuitems");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(260);
            entity.Property(x => x.AccentLabel).HasMaxLength(60);
            entity.Property(x => x.ImageUrl).HasMaxLength(500);
            entity.Property(x => x.Price).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.MaxAdditionalSelections).IsRequired(false);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => new { x.CompanyId, x.MenuCategoryId, x.Name }).IsUnique();

            entity.HasOne(x => x.Tenant)
                .WithMany(x => x.MenuItems)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Company)
                .WithMany(x => x.MenuItems)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.MenuCategory)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.MenuCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Navigation(x => x.AdditionalGroups)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<MenuAdditionalCatalogGroup>(entity =>
        {
            entity.ToTable("menuadditionalcataloggroups");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.AllowMultiple).IsRequired();
            entity.Property(x => x.MaxAdditionalSelections).IsRequired(false);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();

            entity.Navigation(x => x.Options)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<MenuAdditionalCatalogOption>(entity =>
        {
            entity.ToTable("menuadditionalcatalogoptions");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Price).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => new { x.MenuAdditionalCatalogGroupId, x.Name }).IsUnique();

            entity.HasOne(x => x.MenuAdditionalCatalogGroup)
                .WithMany(x => x.Options)
                .HasForeignKey(x => x.MenuAdditionalCatalogGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MenuItemAdditionalGroup>(entity =>
        {
            entity.ToTable("menuitemadditionalgroups");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.AllowMultiple).IsRequired();
            entity.Property(x => x.MaxAdditionalSelections).IsRequired(false);
            entity.Property(x => x.SourceMenuAdditionalCatalogGroupId).IsRequired(false);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => new { x.MenuItemId, x.Name }).IsUnique();
            entity.HasIndex(x => x.SourceMenuAdditionalCatalogGroupId);

            entity.HasOne(x => x.MenuItem)
                .WithMany(x => x.AdditionalGroups)
                .HasForeignKey(x => x.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(x => x.Options)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<MenuItemAdditionalOption>(entity =>
        {
            entity.ToTable("menuitemadditionaloptions");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Price).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.SourceMenuAdditionalCatalogOptionId).IsRequired(false);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => new { x.MenuItemAdditionalGroupId, x.Name }).IsUnique();
            entity.HasIndex(x => x.SourceMenuAdditionalCatalogOptionId);

            entity.HasOne(x => x.MenuItemAdditionalGroup)
                .WithMany(x => x.Options)
                .HasForeignKey(x => x.MenuItemAdditionalGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.MenuItem)
                .WithMany()
                .HasForeignKey(x => x.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SignupCode>(entity =>
        {
            entity.ToTable("signupcodes");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Label).HasMaxLength(120).IsRequired();
            entity.Property(x => x.CodeHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.BoundEmail).HasMaxLength(180);
            entity.Property(x => x.AllowedPlanName).HasMaxLength(120);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.ExpiresAtUtc).IsRequired();

            entity.HasIndex(x => x.CodeHash).IsUnique();
        });

        modelBuilder.Entity<PasswordResetRequest>(entity =>
        {
            entity.ToTable("passwordresetrequests");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.ExpiresAtUtc).IsRequired();

            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.AppUserId, x.IsActive });

            entity.HasOne(x => x.AppUser)
                .WithMany()
                .HasForeignKey(x => x.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SalesAgent>(entity =>
        {
            entity.ToTable("salesagents");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.Code).HasMaxLength(20).IsRequired();
            entity.Property(x => x.CommissionPercent).HasPrecision(5, 2);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.IsActive });

            entity.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomerOrder>(entity =>
        {
            entity.Property(x => x.SalesAgentId).IsRequired(false);
            entity.Property(x => x.SalesOrigin).HasConversion<int>().IsRequired(false);

            entity.HasOne(x => x.SalesAgent)
                .WithMany()
                .HasForeignKey(x => x.SalesAgentId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        });
    }
}
