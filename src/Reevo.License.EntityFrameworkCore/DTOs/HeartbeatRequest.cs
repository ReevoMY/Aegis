namespace Reevo.License.EntityFrameworkCore.DTOs;

public class HeartbeatRequest
{
    public string LicenseKey { get; init; } = string.Empty;
    public string MachineId { get; init; } = string.Empty;
}