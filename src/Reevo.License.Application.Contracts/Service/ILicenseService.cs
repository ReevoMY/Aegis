using Reevo.License.Application.Contracts.Dto;
using Volo.Abp.Application.Services;

namespace Reevo.License.Application.Contracts.Service;

public interface ILicenseService : IApplicationService
{
    /// <summary>
    ///     Generates a license file asynchronously.
    /// </summary>
    public Task<LicenseGenerationResult> GenerateLicenseAsync(LicenseGenerationRequest request);

    /// <summary>
    ///     Validates a license asynchronously.
    /// </summary>
    public Task<LicenseValidationResult> ValidateLicenseAsync(LicenseValidationRequest request);

    /// <summary>
    ///     Activates a license asynchronously.
    /// </summary>
    public Task<LicenseActivationResult> ActivateLicenseAsync(LicenseActivationRequest request);

    /// <summary>
    ///     Revokes a license asynchronously.
    /// </summary>
    public Task<LicenseDeactivationResult> RevokeLicenseAsync(LicenseDeactivationRequest request);

    /// <summary>
    ///    Renews a license asynchronously.
    /// </summary>
    public Task<LicenseRenewalResult> RenewLicenseAsync(LicenseRenewalRequest request);

    /// <summary>
    ///     Processes a heartbeat for a concurrent license asynchronously.
    /// </summary>
    public Task<bool> HeartbeatAsync(HeartbeatRequest request);
}