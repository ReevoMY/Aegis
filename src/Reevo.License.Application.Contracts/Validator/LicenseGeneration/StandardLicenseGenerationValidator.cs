using FluentValidation;
using Reevo.License.Application.Contracts.Dto;
using Reevo.License.Domain.Shared.Enum;

namespace Reevo.License.Application.Contracts.Validator;

public class StandardLicenseGenerationValidator : LicenseGenerationValidator<StandardLicenseGenerationRequest>
{
    public StandardLicenseGenerationValidator(LicenseGenerationErrorCodes errorCodes)
    {
        RuleFor(dto => dto.LicenseType).Equal(LicenseType.Standard)
            .WithMessage(errorCodes.InvalidLicense);

        When(request => request.User != null, () =>
        {
            RuleFor(request => request.User)
                .SetValidator(new LicenseUserDtoValidator()!);
        });
    }
}