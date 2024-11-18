using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Reevo.License.Domain.Shared.Enum;
using Aegis.Server.Enums;
using Ardalis.GuardClauses;
using Reevo.License.Domain.Shared.Model;
using Volo.Abp.Domain.Entities;

namespace Aegis.Server.Entities;

public class License : BasicAggregateRoot<Guid>
{
    #region ctor

    public License()
    {
    }

    public License(Guid id, LicenseType type)
    {
        Guard.Against.NullOrEmpty(id);

        Id = id;
        Type = type;
    }

    #endregion

    #region Properties

    [StringLength(LicenseConsts.MaxLicenseKeyLength)]
    [Column(TypeName = LicenseConsts.LicenseKeyDataType)]
    public string LicenseKey { get; set; } = Guid.NewGuid().ToString("D").ToUpper();

    public LicenseType Type { get; init; }

    [StringLength(LicenseConsts.MaxDescriptionLength)]
    [Column(TypeName = LicenseConsts.DescriptionDataType)]
    public string? Description { get; init; }

    [StringLength(LicenseConsts.MaxVersionLength)]
    [Column(TypeName = LicenseConsts.VersionDataType)]
    public string? Version { get; init; }

    [StringLength(LicenseConsts.MaxIssuerLength)]
    [Column(TypeName = LicenseConsts.IssuerDataType)]
    public string Issuer { get; set; } = string.Empty;

    [StringLength(LicenseConsts.MaxIssuedToLength)]
    [Column(TypeName = LicenseConsts.IssuedToDataType)]
    public string IssuedTo { get; init; } = string.Empty;

    public DateTime IssuedOn { get; init; } = DateTime.UtcNow;

    public DateTime? ExpirationDate { get; set; }

    public LicenseStatus Status { get; set; } = LicenseStatus.Active;

    public int? MaxActiveUsersCount { get; init; }

    public int? ActiveUsersCount { get; set; }

    public string? HardwareId { get; set; } = string.Empty;

    public DateTime? SubscriptionExpiryDate { get; set; }

    #region Navigation properties

    public Guid ProductId { get; init; } = Guid.Empty;
    public Product Product { get; init; } = null!;

    public ICollection<LicenseFeature> LicenseFeatures { get; init; } = [];

    public ICollection<Activation> Activations { get; init; } = [];

    public Guid UserId { get; init; } = Guid.Empty;

    #endregion

    #endregion
}