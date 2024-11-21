using Reevo.License.Application.Contracts.Dto;
using Reevo.License.Application.Contracts.Service;
using Volo.Abp.Application.Services;

namespace Reevo.License.Application.Service;

public class LicenseService : ApplicationService, ILicenseService
{
    public virtual async Task<LicenseGenerationResult> GenerateLicenseAsync(LicenseGenerationRequest request)
    {
        throw new NotImplementedException();
    }

    public virtual async Task<LicenseValidationResult> ValidateLicenseAsync(LicenseValidationRequest request)
    {
        throw new NotImplementedException();
    }

    public virtual async Task<LicenseActivationResult> ActivateLicenseAsync(LicenseActivationRequest request)
    {
        throw new NotImplementedException();
    }

    public virtual async Task<LicenseDeactivationResult> RevokeLicenseAsync(LicenseDeactivationRequest request)
    {
        throw new NotImplementedException();
    }

    public virtual async Task<LicenseRenewalResult> RenewLicenseAsync(LicenseRenewalRequest request)
    {
        throw new NotImplementedException();
    }

    public virtual async Task<bool> HeartbeatAsync(HeartbeatRequest request)
    {
        throw new NotImplementedException();
    }
}