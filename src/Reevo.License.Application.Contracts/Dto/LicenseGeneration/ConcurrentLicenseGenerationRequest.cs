using Reevo.License.Domain.Shared.Enum;

namespace Reevo.License.Application.Contracts.Dto;

/// <summary>
/// Concurrent license generation request.
/// </summary>
public class ConcurrentLicenseGenerationRequest : LicenseGenerationRequest
{
    /// <summary>
    /// Maximum allowed active users.
    /// Set 0 for unlimited.
    /// </summary>
    public virtual int MaxActiveUsersCount { get; init; }

    /// <summary>
    /// Maximum allowed concurrent users.
    /// Set 0 for unlimited.
    /// </summary>
    public virtual int MaxConcurrentUsersCount { get; init; }

    /// <summary>
    /// The users the license is issued to. (optional)
    /// </summary>
    public virtual LicenseUserDto[]? Users { get; init; }

    public override LicenseType LicenseType => LicenseType.Concurrent;
}