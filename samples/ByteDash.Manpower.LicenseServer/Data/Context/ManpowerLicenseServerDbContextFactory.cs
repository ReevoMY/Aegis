using ByteDash.Manpower.LicenseServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ByteDash.Manpower.LicenseServer;

public class ManpowerLicenseServerDbContextFactory : IDesignTimeDbContextFactory<ManpowerLicenseServerDbContext>
{
    public ManpowerLicenseServerDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();

        var builder = new DbContextOptionsBuilder<ManpowerLicenseServerDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));

        return new ManpowerLicenseServerDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false);

        return builder.Build();
    }
}