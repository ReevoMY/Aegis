namespace Reevo.License.Application.Contracts.Dto;

public class LicenseDeactivationResult(bool isSuccessful, string? exception = null)
{
    public bool IsSuccessful { get; } = isSuccessful;

    public string? Exception { get; } = exception;
}