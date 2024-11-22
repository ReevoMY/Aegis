using Volo.Abp.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace ByteDash.Manpower.LicenseServer.Data;

public class LicenseServerDbSchemaMigrator : ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public LicenseServerDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        
        /* We intentionally resolving the LicenseServerDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<LicenseServerDbContext>()
            .Database
            .MigrateAsync();

    }
}
