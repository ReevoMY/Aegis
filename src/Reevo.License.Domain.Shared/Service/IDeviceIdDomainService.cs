using Volo.Abp.Domain.Services;

namespace Reevo.License.Domain.Shared.Service;

public interface IDeviceIdDomainService : IDomainService
{
    /// <summary>
    ///     Gets a unique device identifier for the current machine.
    /// </summary>
    /// <returns>A string representing the device identifier.</returns>
    /// <remarks>
    ///     The implementation of this method uses a simple <a href="https://github.com/MatthewKing/DeviceId?tab=readme-ov-file#building-a-device-identifier">DeviceIdBuilder</a>.
    ///     Please consider using a more secure method for cross-platform compatibility considerations.
    /// </remarks>
    public Task<string> GetDeviceIdAsync();

    /// <summary>
    ///     Verifies a device identifier against the current machine's device identifier.
    /// </summary>
    /// <param name="deviceId">The device identifier to validate.</param>
    /// <returns>True if the hardware identifier matches, false otherwise.</returns>
    public Task<bool> VerifyDeviceIdAsync(string deviceId);
}