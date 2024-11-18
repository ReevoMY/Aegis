using Microsoft.Extensions.DependencyInjection;
using Reevo.License.EntityFrameworkCore.Services;

namespace Reevo.License.EntityFrameworkCore.Extensions;

public static class ServiceExtensions
{
    public static void AddAegisServer(this IServiceCollection services)
    {
        services.AddHostedService<HeartbeatMonitor>();
        services.AddScoped<LicenseService>();
    }
}