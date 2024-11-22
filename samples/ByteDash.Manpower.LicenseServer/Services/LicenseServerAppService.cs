using Volo.Abp.Application.Services;
using ByteDash.Manpower.LicenseServer.Localization;

namespace ByteDash.Manpower.LicenseServer.Services;

/* Inherit your application services from this class. */
public abstract class LicenseServerAppService : ApplicationService
{
    protected LicenseServerAppService()
    {
        LocalizationResource = typeof(LicenseServerResource);
    }
}