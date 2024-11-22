using Microsoft.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace ByteDash.Manpower.LicenseServer.Data;

public class ManpowerLicenseServerDbContext(DbContextOptions<ManpowerLicenseServerDbContext> options) :
    AbpDbContext<ManpowerLicenseServerDbContext>(options)
{
    //public DbSet<User> Users { get; init; }
    //public DbSet<Role> Roles { get; init; }
    //public DbSet<RefreshToken> RefreshTokens { get; init; }

    //protected override void OnModelCreating(ModelBuilder modelBuilder)
    //{
    //    base.OnModelCreating(modelBuilder);

    //    // RefreshToken - User (One-to-One)
    //    modelBuilder.Entity<RefreshToken>()
    //        .HasOne(r => r.User)
    //        .WithOne(u => u.RefreshToken)
    //        .HasForeignKey<RefreshToken>(r => r.UserId)
    //        .OnDelete(DeleteBehavior.Cascade);

    //    // User - License (One-to-Many)
    //    modelBuilder.Entity<User>()
    //        .HasMany(u => u.Licenses)
    //        .WithOne()
    //        .HasForeignKey(l => l.UserId)
    //        .OnDelete(DeleteBehavior.Cascade);

    //    base.OnModelCreating(modelBuilder);
    //}
}