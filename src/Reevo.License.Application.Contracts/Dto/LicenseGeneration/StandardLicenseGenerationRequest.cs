using Reevo.License.Domain.Shared.Enum;

namespace Reevo.License.Application.Contracts.Dto;

public class StandardLicenseGenerationRequest : LicenseGenerationRequest
{
    /// <summary>
    /// The user the license is issued to.
    /// </summary>
    public virtual LicenseUserDto? User { get; init; }

    public override LicenseType LicenseType => LicenseType.Standard;
}