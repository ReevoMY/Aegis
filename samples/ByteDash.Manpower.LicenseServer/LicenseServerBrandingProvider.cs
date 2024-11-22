using Microsoft.Extensions.Localization;
using ByteDash.Manpower.LicenseServer.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace ByteDash.Manpower.LicenseServer;

[Dependency(ReplaceServices = true)]
public class LicenseServerBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<LicenseServerResource> _localizer;

    public LicenseServerBrandingProvider(IStringLocalizer<LicenseServerResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}