using Reevo.License.Domain.Shared.Enum;

namespace Reevo.License.Application.Contracts.Dto.LicenseGeneration;

public class NodeLockedLicenseGenerationRequest : LicenseGenerationRequest
{
    /// <summary>
    /// The user the license is issued to.
    /// </summary>
    public virtual LicenseUserDto? User { get; init; }

    public virtual string HardwareId { get; init; } = null!;

    public override LicenseType LicenseType => LicenseType.NodeLocked;
}