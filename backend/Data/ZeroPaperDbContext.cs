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
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<SignupCode> SignupCodes => Set<SignupCode>();
    public DbSet<PasswordResetRequest> PasswordResetRequests => Set<PasswordResetRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasCharSet("utf8mb4");

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("Tenants");
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
            entity.ToTable("Companies");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.LegalName).HasMaxLength(180).IsRequired();
            entity.Property(x => x.TradeName).HasMaxLength(180).IsRequired();
            entity.Property(x => x.AccessSlug).HasMaxLength(80).IsRequired();
            entity.Property(x => x.DocumentNumber).HasMaxLength(30);
            entity.Property(x => x.ContactEmail).HasMaxLength(180);
            entity.Property(x => x.ContactPhone).HasMaxLength(30);
            entity.Property(x => x.LastOrderNumber).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => new { x.TenantId, x.AccessSlug }).IsUnique();

            entity.HasOne(x => x.Tenant)
                .WithMany(x => x.Companies)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(180).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Role)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();

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
            entity.ToTable("Sessions");
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
            entity.ToTable("Subscriptions");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.PlanName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.MonthlyPrice).HasPrecision(10, 2).IsRequired();
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
            entity.ToTable("QrCodeAccesses");
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
            entity.ToTable("DiningTables");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.InternalCode).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => new { x.CompanyId, x.InternalCode }).IsUnique();
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
            entity.ToTable("CustomerOrders");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.CustomerName).HasMaxLength(120);
            entity.Property(x => x.Notes).HasMaxLength(600);
            entity.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.PaymentMethod)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.PaymentStatus)
                .HasConversion<int>()
                .IsRequired();
            entity.Property(x => x.TotalAmount).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.SubmittedAtUtc).IsRequired();

            entity.HasIndex(x => new { x.CompanyId, x.Number }).IsUnique();

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

            entity.Navigation(x => x.Items)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Quantity).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.UnitPrice).HasPrecision(10, 2).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(300);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasOne(x => x.CustomerOrder)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.CustomerOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StockItem>(entity =>
        {
            entity.ToTable("StockItems");
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
            entity.ToTable("MenuCategories");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
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
            entity.ToTable("MenuItems");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(260);
            entity.Property(x => x.AccentLabel).HasMaxLength(60);
            entity.Property(x => x.ImageUrl).HasMaxLength(500);
            entity.Property(x => x.Price).HasPrecision(10, 2).IsRequired();
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
        });

        modelBuilder.Entity<SignupCode>(entity =>
        {
            entity.ToTable("SignupCodes");
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
            entity.ToTable("PasswordResetRequests");
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
    }
}
