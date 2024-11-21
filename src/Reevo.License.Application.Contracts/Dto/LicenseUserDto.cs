namespace Reevo.License.Application.Contracts.Dto;

/// <summary>
/// The license user information.
/// </summary>
public class LicenseUserDto
{
    /// <summary>
    /// The user's name the license is issued to.
    /// </summary>
    public virtual string IssuedTo { get; set; } = string.Empty;


    /// <summary>
    /// The username of the license user issued to.
    /// </summary>
    public virtual string UserName { get; set; } = string.Empty;


    /// <summary>
    /// The reference user id of the software that the license is issued to.
    /// </summary>
    public virtual string? ReferenceUserId { get; set; }

    /// <summary>
    /// The user's email the license is issued to.
    /// </summary>
    public virtual string? Email { get; set; }

    /// <summary>
    /// The user's IP address the license is issued to.
    /// </summary>
    public virtual string? IpAddress { get; set; }

    /// <summary>
    /// Determines if the user's IP address has been validated.
    /// </summary>
    public virtual bool IsUserIpAddressValidated { get; init; }
}