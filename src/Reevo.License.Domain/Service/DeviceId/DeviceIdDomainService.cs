using DeviceId;
using DeviceId.Encoders;
using Reevo.License.Domain.Shared.Service;
using Volo.Abp.Domain.Services;

namespace Reevo.License.Domain.Service;

public class DeviceIdDomainService : DomainService, IDeviceIdDomainService
{
    #region Fields

    public IDeviceIdFormatter DeviceIdFormatter { get; protected init; }

    #endregion

    public DeviceIdDomainService()
    {
        DeviceIdFormatter = DeviceIdFormatters.DefaultV6;
    }

    public DeviceIdDomainService(IDeviceIdFormatter deviceIdFormatter)
    {
        DeviceIdFormatter = deviceIdFormatter;
    }

    #region Methods

    public virtual Task<string> GetDeviceIdAsync()
    {
        var deviceId = new DeviceIdBuilder()
            .AddMachineName()
            .AddMacAddress(true, true)
            .AddFileToken("example-device-token.txt")
            .UseFormatter(DeviceIdFormatter)
            .ToString();

        return Task.FromResult(deviceId);
    }

    public virtual Task<bool> VerifyDeviceIdAsync(string deviceId)
    {
        if (DeviceIdFormatter.GetType() != DeviceIdFormatters.DefaultV6.GetType())
        {
            throw new InvalidOperationException("Custom DeviceId formatter is used. " +
                                                "Please override the method to include your own validation logic.");
        }

        // https://github.com/MatthewKing/DeviceId/issues/68
        var isValid = deviceId.Length == 52 && deviceId.All(c => Base32ByteArrayEncoder.CrockfordAlphabet.Contains(c));

        // return await GetDeviceIdAsync() == deviceId;
        return Task.FromResult(isValid);
    }

    #endregion
}