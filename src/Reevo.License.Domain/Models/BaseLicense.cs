using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Reevo.License.Domain.Shared.Enum;

[assembly: InternalsVisibleTo("Reevo.License.EntityFrameworkCore")]
[assembly: InternalsVisibleTo("Reevo.License.Domain.Tests")]
[assembly: InternalsVisibleTo("Sample.License.Web.Tests")]

namespace Reevo.License.Domain.Models;

[JsonDerivedType(typeof(StandardLicense), "Standard")]
[JsonDerivedType(typeof(TrialLicense), "Trial")]
[JsonDerivedType(typeof(NodeLockedLicense), "NodeLocked")]
[JsonDerivedType(typeof(SubscriptionLicense), "Subscription")]
[JsonDerivedType(typeof(FloatingLicense), "Floating")]
[JsonDerivedType(typeof(ConcurrentLicense), "Concurrent")]
public class BaseLicense
{
    [JsonInclude] public Guid LicenseId { get; internal init; } = Guid.NewGuid();

    [JsonInclude] public string LicenseKey { get; internal set; } = Guid.NewGuid().ToString("D").ToUpper();

    [JsonInclude] public LicenseType Type { get; init; }

    [JsonInclude] public DateTime IssuedOn { get; internal init; } = DateTime.UtcNow;

    [JsonInclude] public DateTime? ExpirationDate { get; protected internal set; }

    [JsonInclude] public Dictionary<string, bool> Features { get; protected internal set; } = new();

    [JsonInclude] public string Issuer { get; protected internal set; } = string.Empty;

    [JsonInclude] public string Description { get; protected internal set; } = string.Empty;
}
