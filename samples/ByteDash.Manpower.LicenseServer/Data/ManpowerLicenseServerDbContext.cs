using ByteDash.Manpower.LicenseServer.Entities.License;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace ByteDash.Manpower.LicenseServer.Data;

public class ManpowerLicenseServerDbContext(DbContextOptions<ManpowerLicenseServerDbContext> options)
    : AbpDbContext<ManpowerLicenseServerDbContext>(options)
{
    public const string DbTablePrefix = "App";
    public const string DbSchema = null!;

    #region Properties

    public DbSet<ManpowerLicense> Licenses { get; set; }

    #endregion

    protected override void OnModelCreating(ModelBuilder builder)
    {
        //builder.ConfigureTenantManagement();

        builder.Entity<ManpowerLicense>(b =>
        {
            b.ToTable(DbTablePrefix + nameof(Licenses),
                DbSchema);
            b.ConfigureByConvention();
        });
    }
}