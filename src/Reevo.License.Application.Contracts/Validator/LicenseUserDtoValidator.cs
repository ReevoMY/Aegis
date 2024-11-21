using FluentValidation;
using Reevo.License.Application.Contracts.Dto;
using System.Net;

namespace Reevo.License.Application.Contracts.Validator;

public class LicenseUserDtoValidator<T> : AbstractValidator<T>
    where T : LicenseUserDto
{
    public LicenseUserDtoValidator()
    {
        RuleFor(user => user.IssuedTo).NotEmpty();
        RuleFor(user => user.UserName).NotEmpty();

        When(request => !request.Email.IsNullOrEmpty(), () =>
        {
            RuleFor(request => request.Email)
                .NotEmpty()
                .EmailAddress();
        });

        When(request => !request.IpAddress.IsNullOrEmpty(), () =>
        {
            RuleFor(user => user.IpAddress)
                .NotEmpty()
                .Must(CheckValidIpAddress!);
        });
    }

    #region Private

    private bool CheckValidIpAddress(string ipAddress)
    {
        return IPAddress.TryParse(ipAddress, out _);
    }

    #endregion
}

public class LicenseUserDtoValidator : LicenseUserDtoValidator<LicenseUserDto>
{
}