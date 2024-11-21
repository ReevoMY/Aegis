using Reevo.License.Domain.Shared.Enum;
using Volo.Abp.ObjectExtending;

namespace Reevo.License.Application.Contracts.Dto;

public class LicenseGenerationResult : ExtensibleObject
{
    /// <summary>
    /// A byte array containing the generated license file.
    /// </summary>
    public virtual byte[] LicenseFile { get; set; } = null!;

    public virtual LicenseType Type { get; set; }

    public virtual DateTime? IssuedOn { get; set; }

    public virtual string? IssuedBy { get; set; }
}