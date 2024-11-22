using Reevo.License.Domain.Shared.Enum;
using Reevo.License.Domain.Shared.Model;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities;

namespace ByteDash.Manpower.LicenseServer.Entities.License;

public class ManpowerLicense : BasicAggregateRoot<Guid>
{
    [StringLength(LicenseConsts.MaxLicenseKeyLength)]
    [Column(TypeName = LicenseConsts.LicenseKeyDataType)]
    public string LicenseKey { get; set; } = null!;

    public LicenseType LicenseType { get; set; }

    [StringLength(LicenseConsts.MaxDescriptionLength)]
    [Column(TypeName = LicenseConsts.DescriptionDataType)]
    public string? Description { get; set; }

    [StringLength(LicenseConsts.MaxVersionLength)]
    [Column(TypeName = LicenseConsts.VersionDataType)]
    public string? Version { get; set; }

    [StringLength(LicenseConsts.MaxIssuerLength)]
    [Column(TypeName = LicenseConsts.IssuerDataType)]
    public string Issuer { get; set; } = null!;

    [StringLength(LicenseConsts.MaxIssuedToLength)]
    [Column(TypeName = LicenseConsts.IssuedToDataType)]
    public string IssuedTo { get; set; } = null!;

    public DateTime IssuedOn { get; set; }

    public DateTime? ExpirationDate { get; set; }

    public LicenseStatus Status { get; set; }

    public int? MaxActiveUsersCount { get; set; }

    public int? ActiveUsersCount { get; set; }

    [StringLength(LicenseConsts.MaxDeviceIdLength)]
    [Column(TypeName = LicenseConsts.DeviceIdDataType)]
    public string? DeviceId { get; set; }
}