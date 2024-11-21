using System.ComponentModel.DataAnnotations;

namespace Reevo.License.Application.Contracts.Dto;

public class LicenseValidationRequest
{
    /// <summary>
    /// The license key to validate.
    /// </summary>
    [Required]
    public string LicenseKey { get; set; } = null!;

    /// <summary>
    /// The license file to validate.
    /// </summary>
    [Required] 
    public byte[] LicenseFile { get; set; } = null!;

    /// <summary>
    /// Optional validation parameters.
    /// </summary>
    public Dictionary<string, string?>? ValidationParams { get; set; }
}