using ByteDash.Manpower.LicenseServer.Entities.License;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace ByteDash.Manpower.LicenseServer.Data;

public class ManpowerLicenseServerDbContext(DbContextOptions<ManpowerLicenseServerDbContext> options)
    : DbContext(options)
    //: AbpDbContext<ManpowerLicenseServerDbContext>(options)
{
    #region Properties

    public DbSet<ManpowerLicense> Licenses { get; set; }

    #endregion
}