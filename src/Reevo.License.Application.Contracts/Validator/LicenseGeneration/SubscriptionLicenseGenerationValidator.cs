using FluentValidation;
using Reevo.License.Application.Contracts.Dto.LicenseGeneration;
using Reevo.License.Domain.Shared.Enum;

namespace Reevo.License.Application.Contracts.Validator.LicenseGeneration;

public class SubscriptionLicenseGenerationValidator : LicenseGenerationValidator<SubscriptionLicenseGenerationRequest>
{
    public SubscriptionLicenseGenerationValidator(LicenseGenerationErrorCodes errorCodes)
    {
        RuleFor(dto => dto.LicenseType).Equal(LicenseType.Subscription)
            .WithMessage(errorCodes.InvalidLicense);

        When(request => request.User != null, () =>
        {
            RuleFor(request => request.User)
                .SetValidator(new LicenseUserDtoValidator()!);
        });

        RuleFor(request => request.SubscriptionStartDate)
            .NotEmpty()
            .LessThanOrEqualTo(DateTime.Today)
            .WithMessage("SubscriptionStartDate cannot be later than today.");

        RuleFor(request => request.SubscriptionDuration)
            .NotEmpty()
            .LessThanOrEqualTo(TimeSpan.Zero)
            .WithMessage("SubscriptionDuration cannot be lesser than a day.");
    }
}