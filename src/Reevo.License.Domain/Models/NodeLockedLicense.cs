using System.Text.Json.Serialization;
using Reevo.License.Domain.Shared.Enum;

namespace Reevo.License.Domain.Models;

[JsonDerivedType(typeof(NodeLockedLicense), "NodeLocked")]
public class NodeLockedLicense : BaseLicense
{
    [JsonConstructor]
    public NodeLockedLicense()
    {
        HardwareId = string.Empty;
        Type = LicenseType.NodeLocked;
    }

    public NodeLockedLicense(string hardwareId)
    {
        HardwareId = hardwareId;
        Type = LicenseType.NodeLocked;
    }

    public NodeLockedLicense(BaseLicense license, string hardwareId)
    {
        HardwareId = hardwareId;
        Type = LicenseType.NodeLocked;
        ExpirationDate = license.ExpirationDate;
        Features = license.Features;
        Issuer = license.Issuer;
        LicenseId = license.LicenseId;
        LicenseKey = license.LicenseKey;
        Type = license.Type;
        IssuedOn = license.IssuedOn;
    }

    [JsonInclude]
    public LicenseUser? User { get; protected init; }

    [JsonInclude]
    public string HardwareId { get; protected init; }

    [JsonInclude]
    public bool ValidateUserIpAddress { get; protected init; }
}