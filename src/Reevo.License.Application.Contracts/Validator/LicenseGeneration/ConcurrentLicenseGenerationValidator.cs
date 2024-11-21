using FluentValidation;
using Reevo.License.Application.Contracts.Dto;
using Reevo.License.Domain.Shared.Enum;

namespace Reevo.License.Application.Contracts.Validator.LicenseGeneration;

public class ConcurrentLicenseGenerationValidator : LicenseGenerationValidator<ConcurrentLicenseGenerationRequest>
{
    public ConcurrentLicenseGenerationValidator(LicenseGenerationErrorCodes errorCodes)
    {
        RuleFor(dto => dto.LicenseType).Equal(LicenseType.Concurrent)
            .WithMessage(errorCodes.InvalidLicense);

        RuleFor(dto => dto.MaxActiveUsersCount).GreaterThanOrEqualTo(0);

        RuleFor(dto => dto.MaxConcurrentUsersCount).GreaterThanOrEqualTo(0);

        When(request => !request.Users.IsNullOrEmpty(), () =>
        {
            RuleFor(request => request.Users)
                .ForEach(user => user.SetValidator(new LicenseUserDtoValidator()));
        });
    }
}