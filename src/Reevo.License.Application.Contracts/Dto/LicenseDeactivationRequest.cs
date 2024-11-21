using System.ComponentModel.DataAnnotations;

namespace Reevo.License.Application.Contracts.Dto;

public class LicenseDeactivationRequest
{
    /// <summary>
    /// The license key to revoke.
    /// </summary>
    [Required]
    public string LicenseKey { get; set; } = null!;

    /// <summary>
    /// The hardware ID of the machine to revoke the license from (optional).
    /// </summary>
    public string? HardwareId { get; set; }
}