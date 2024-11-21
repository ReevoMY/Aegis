using System.Text.Json.Serialization;
using Reevo.License.Domain.Shared.Enum;

namespace Reevo.License.Domain.Models;

[JsonDerivedType(typeof(StandardLicense), nameof(LicenseType.Standard))]
public class StandardLicense : BaseLicense
{
    [JsonConstructor]
    protected StandardLicense()
    {
        Type = LicenseType.Standard;
    }

    public StandardLicense(string userName)
    {
        UserName = userName;
        Type = LicenseType.Standard;
    }

    public StandardLicense(BaseLicense license, string userName)
    {
        UserName = userName;
        Type = LicenseType.Standard;
        ExpirationDate = license.ExpirationDate;
        Features = license.Features;
        Issuer = license.Issuer;
        LicenseId = license.LicenseId;
        LicenseKey = license.LicenseKey;
        Type = license.Type;
        IssuedOn = license.IssuedOn;
    }

    [JsonInclude]
    public string UserName { get; protected internal set; } = string.Empty;
}