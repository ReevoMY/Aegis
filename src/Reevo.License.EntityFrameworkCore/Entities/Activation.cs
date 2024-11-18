using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Reevo.License.Domain.Shared.Enum;
using Reevo.License.Domain.Shared.Model;
using Volo.Abp.Domain.Entities;

namespace Reevo.License.EntityFrameworkCore.Entities;

public class Activation : BasicAggregateRoot<Guid>
{
    public Guid LicenseId { get; set; }
    public License License { get; set; } = null!;

    [StringLength(ActivationConsts.MaxMachineIdLength)]
    [Column(TypeName = ActivationConsts.MachineIdDataType)]
    public string MachineId { get; set; } = string.Empty;

    public DateTime ActivationDate { get; init; } = DateTime.UtcNow;

    public ActivationMode ActivationMode { get; init; }

    public DateTime LastHeartbeat { get; set; }

    public Guid UserId { get; set; }
}