using Reevo.License.Domain.Shared.Enum;

namespace Reevo.License.Application.Contracts.Dto.LicenseGeneration;

public class SubscriptionLicenseGenerationRequest : LicenseGenerationRequest
{
    /// <summary>
    /// The user the license is issued to.
    /// </summary>
    public virtual LicenseUserDto? User { get; init; }

    public virtual DateTime SubscriptionStartDate { get; protected internal set; }

    public virtual TimeSpan SubscriptionDuration { get; protected internal set; }

    public override LicenseType LicenseType => LicenseType.Subscription;
}