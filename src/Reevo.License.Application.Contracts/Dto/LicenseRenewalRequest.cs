using System.ComponentModel.DataAnnotations;

namespace Reevo.License.Application.Contracts.Dto;

public class LicenseRenewalRequest
{
    /// <summary>
    /// The license key to renew.
    /// </summary>
    [Required]
    public string LicenseKey { get; set; } = null!;

    /// <summary>
    /// The new expiration date for the license.
    /// </summary>
    public DateOnly? NewExpirationDate { get; set; }
}