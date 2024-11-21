using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Volo.Abp.DependencyInjection;

namespace Reevo.License.Application.Contracts.Validator;

/// <summary>
/// Error codes for license generation.
/// </summary>
[Dependency(ServiceLifetime.Transient, ReplaceServices = true)]
public class LicenseGenerationErrorCodes
{
    #region Fields

    private readonly IStringLocalizer<LicenseGenerationErrorCodes> _localizer;

    #endregion

    public LicenseGenerationErrorCodes(IStringLocalizer<LicenseGenerationErrorCodes> localizer)
    {
        _localizer = localizer;
    }

    public string InvalidLicense => _localizer["License.LicenseGeneration.Error.InvalidLicense"];
}