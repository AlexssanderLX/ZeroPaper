using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ZeroPaper.Data;

public class ZeroPaperDbContextFactory : IDesignTimeDbContextFactory<ZeroPaperDbContext>
{
    public ZeroPaperDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var basePath = Directory.GetCurrentDirectory();

        if (Path.GetFileName(basePath).Equals("backend", StringComparison.OrdinalIgnoreCase) is false)
        {
            basePath = Path.Combine(basePath, "backend");
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddUserSecrets<ZeroPaperDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection was not configured.");

        var optionsBuilder = new DbContextOptionsBuilder<ZeroPaperDbContext>();

        optionsBuilder.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString));

        return new ZeroPaperDbContext(optionsBuilder.Options);
    }
}
