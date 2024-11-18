namespace Reevo.License.EntityFrameworkCore.DTOs;

public class RenewLicenseRequest
{
    public string LicenseKey { get; init; } = string.Empty;
    public DateTime NewExpirationDate { get; init; }
}