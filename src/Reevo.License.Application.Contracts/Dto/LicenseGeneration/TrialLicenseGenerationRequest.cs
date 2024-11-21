using Reevo.License.Domain.Shared.Enum;

namespace Reevo.License.Application.Contracts.Dto.LicenseGeneration;

public class TrialLicenseGenerationRequest : LicenseGenerationRequest
{
    /// <summary>
    /// The user the license is issued to.
    /// </summary>
    public virtual LicenseUserDto? User { get; init; }

    /// <summary>
    /// The trial period for the trial license.
    /// </summary>
    public virtual TimeSpan? TrialPeriod { get; init; }

    public virtual DateTime? ValidFrom { get; init; }

    public virtual DateTime? ValidTo { get; init; }

    public override LicenseType LicenseType => LicenseType.Floating;
}