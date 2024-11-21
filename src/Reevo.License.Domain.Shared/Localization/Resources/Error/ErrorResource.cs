using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Localization;

namespace Reevo.License.Domain.Shared.Localization;

/// <summary>
/// Error codes for license generation.
/// </summary>
[Dependency(ServiceLifetime.Transient)]
[LocalizationResourceName("Reevo.License.Error")]
public class ErrorResource
{
    #region Fields

    private readonly IStringLocalizer<ErrorResource> _localizer;

    #endregion

    public ErrorResource(IStringLocalizer<ErrorResource> localizer)
    {
        _localizer = localizer;
    }

    public string RequiredError(string parameter) => _localizer["Error.Required", parameter];
}