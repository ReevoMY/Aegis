using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement.EntityFrameworkCore;

namespace ByteDash.Manpower.LicenseServer.Data;

public class ManpowerLicenseServerDbContext(DbContextOptions<ManpowerLicenseServerDbContext> options) :
    AbpDbContext<ManpowerLicenseServerDbContext>(options)
{
    #region Fields

    public const string DbTablePrefix = "App";
    public const string DbSchema = null;

    #endregion

    //public DbSet<User> Users { get; init; }
    //public DbSet<Role> Roles { get; init; }
    //public DbSet<RefreshToken> RefreshTokens { get; init; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */
        builder.ConfigureSettingManagement();
        //builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureFeatureManagement();
        builder.ConfigurePermissionManagement();
        //builder.ConfigureBlobStoring();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureTenantManagement();

        //// RefreshToken - User (One-to-One)
        //modelBuilder.Entity<RefreshToken>()
        //    .HasOne(r => r.User)
        //    .WithOne(u => u.RefreshToken)
        //    .HasForeignKey<RefreshToken>(r => r.UserId)
        //    .OnDelete(DeleteBehavior.Cascade);

        //// User - License (One-to-Many)
        //modelBuilder.Entity<User>()
        //    .HasMany(u => u.Licenses)
        //    .WithOne()
        //    .HasForeignKey(l => l.UserId)
        //    .OnDelete(DeleteBehavior.Cascade);

        //base.OnModelCreating(modelBuilder);
    }
}