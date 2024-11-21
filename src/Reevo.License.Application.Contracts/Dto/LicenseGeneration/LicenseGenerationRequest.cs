using System.ComponentModel.DataAnnotations;
using Reevo.License.Domain.Shared.Enum;

namespace Reevo.License.Application.Contracts.Dto;

public class LicenseGenerationRequest
{
    /// <summary>
    /// The type of license to generate.
    /// </summary>
    public virtual LicenseType LicenseType { get; set; }

    /// <summary>
    /// The expiration date of the license. (optional)
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// The user's name the license is issued to.
    /// </summary>
    [Required]
    public virtual string IssuedTo { get; init; } = string.Empty;

    /// <summary>
    /// The product id the license is for. (optional)
    /// </summary>
    public virtual Guid? ProductId { get; init; }

    /// <summary>
    /// The feature ids the license is for. (optional)
    /// </summary>
    public virtual Guid[]? FeatureIds { get; init; }
}