namespace Reevo.License.Application.Contracts.Dto;

public class LicenseValidationResult(bool isValid, string? exception = null)
{
    public bool IsValid { get; } = isValid;

    public string? Exception { get; } = exception;
}