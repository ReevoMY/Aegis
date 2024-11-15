using Aegis.Enums;
using Aegis.Server.Enums;
using Volo.Abp.Domain.Entities;

namespace Aegis.Server.Entities;

public class License : BasicAggregateRoot<Guid>
{
    public string LicenseKey { get; set; } = Guid.NewGuid().ToString("D").ToUpper();

    public LicenseType Type { get; init; }

    public DateTime IssuedOn { get; init; } = DateTime.UtcNow;

    public DateTime? ExpirationDate { get; set; }

    public string Issuer { get; set; } = string.Empty;

    public string IssuedTo { get; init; } = string.Empty;

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
}