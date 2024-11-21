using FluentValidation;
using Reevo.License.Application.Contracts.Dto.LicenseGeneration;
using Reevo.License.Domain.Shared.Enum;

namespace Reevo.License.Application.Contracts.Validator.LicenseGeneration;

public class SubscriptionLicenseGenerationValidator : LicenseGenerationValidator<NodeLockedLicenseGenerationRequest>
{
    public SubscriptionLicenseGenerationValidator(LicenseGenerationErrorCodes errorCodes)
    {
        RuleFor(dto => dto.LicenseType).Equal(LicenseType.NodeLocked)
            .WithMessage(errorCodes.InvalidLicense);

        When(request => request.User != null, () =>
        {
            RuleFor(request => request.User)
                .SetValidator(new LicenseUserDtoValidator()!);
        });
    }
}