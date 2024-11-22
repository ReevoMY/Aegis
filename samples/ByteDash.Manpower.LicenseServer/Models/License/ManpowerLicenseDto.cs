using Reevo.License.Domain.Shared.Enum;

namespace ByteDash.Manpower.LicenseServer.Models.License;

public class ManpowerLicenseDto
{
    public Guid Id { get; set; }

    public string LicenseKey { get; set; } = null!;

    public string? Description { get; set; }

    public string? Version { get; set; }

    public string Issuer { get; set; } = null!;

    public string IssuedTo { get; set; } = null!;

    public DateTime IssuedOn { get; set; }

    public DateTime? ExpirationDate { get; set; }

    public LicenseStatus Status { get; set; }

    public int? MaxActiveUsersCount { get; init; }

    public int? ActiveUsersCount { get; set; }
}