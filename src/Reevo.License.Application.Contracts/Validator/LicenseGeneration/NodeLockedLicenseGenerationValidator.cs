using FluentValidation;
using Reevo.License.Application.Contracts.Dto.LicenseGeneration;
using Reevo.License.Domain.Shared.Enum;
using Reevo.License.Domain.Shared.Service;
using Volo.Abp.Threading;

namespace Reevo.License.Application.Contracts.Validator;

public class NodeLockedLicenseGenerationValidator : LicenseGenerationValidator<NodeLockedLicenseGenerationRequest>
{
    public NodeLockedLicenseGenerationValidator(LicenseGenerationErrorCodes errorCodes,
        IDeviceIdDomainService deviceIdDomainService)
    {
        RuleFor(dto => dto.LicenseType).Equal(LicenseType.NodeLocked)
            .WithMessage(errorCodes.InvalidLicense);

        RuleFor(request => request.HardwareId)
            .Must(hardwareId => AsyncHelper.RunSync(() => deviceIdDomainService.VerifyDeviceIdAsync(hardwareId)));

        When(request => request.User != null, () =>
        {
            RuleFor(request => request.User)
                .SetValidator(new LicenseUserDtoValidator()!);
        });
    }
}