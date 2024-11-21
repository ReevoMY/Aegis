using FluentValidation;
using Reevo.License.Application.Contracts.Dto;

namespace Reevo.License.Application.Contracts.Validator;

public class LicenseGenerationValidator<T> : AbstractValidator<T>
    where T : LicenseGenerationRequest
{
    public LicenseGenerationValidator()
    {
        RuleFor(dto => dto.LicenseType).NotEmpty();
        RuleFor(dto => dto.IssuedTo).NotEmpty();
    }
}