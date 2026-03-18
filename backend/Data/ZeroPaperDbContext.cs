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
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<QrCodeAccess> QrCodeAccesses => Set<QrCodeAccess>();

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
    }
}
