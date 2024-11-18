using Reevo.License.EntityFrameworkCore.Entities;

namespace Reevo.License.EntityFrameworkCore.DTOs;

public class LicenseValidationResult(bool isValid, Entities.License? license, Exception? exception = null)
{
    public bool IsValid { get; } = isValid;
    public Entities.License? License { get; } = license;
    public Exception? Exception { get; } = exception;
}