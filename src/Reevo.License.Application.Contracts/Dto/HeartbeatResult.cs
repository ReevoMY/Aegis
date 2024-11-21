namespace Reevo.License.Application.Contracts.Dto;

public class HeartbeatResult(bool isSuccessful, string? exception)
{
    /// <summary>
    /// True if the heartbeat was processed successfully, false otherwise.
    /// </summary>
    public bool IsSuccessful { get; } = isSuccessful;

    public string? Exception { get; } = exception;
}