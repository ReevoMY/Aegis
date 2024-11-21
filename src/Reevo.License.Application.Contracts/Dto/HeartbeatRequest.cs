using System.ComponentModel.DataAnnotations;

namespace Reevo.License.Application.Contracts.Dto;

public class HeartbeatRequest
{
    /// <summary>
    /// The license key.
    /// </summary>
    [Required]
    public string LicenseKey { get; set; } = null!;

    /// <summary>
    /// The machine ID sending the heartbeat.
    /// </summary>
    [Required]
    public string MachineId { get; set; } = null!;
}