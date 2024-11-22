using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ByteDash.Manpower.LicenseServer.Data;

public class LicenseServerDbContextFactory : IDesignTimeDbContextFactory<LicenseServerDbContext>
{
    public LicenseServerDbContext CreateDbContext(string[] args)
    {
        LicenseServerEfCoreEntityExtensionMappings.Configure();
        var configuration = BuildConfiguration();

        var builder = new DbContextOptionsBuilder<LicenseServerDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));

        return new LicenseServerDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false);

        return builder.Build();
    }
}