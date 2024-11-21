using System.ComponentModel.DataAnnotations;

namespace Reevo.License.Application.Contracts.Dto;

public class LicenseActivationRequest
{
    /// <summary>
    /// The license key to activate.
    /// </summary>
    [Required]
    public string LicenseKey { get; set; } = null!;

    /// <summary>
    /// The hardware ID of the machine activating the license (optional).
    /// </summary>
    public string? HardwareId { get; set; }
}