using DeviceId;
using DeviceId.Encoders;
using Reevo.License.Domain.Service;
using Reevo.License.Domain.Shared.Service;
using Volo.Abp.DependencyInjection;

namespace Sample.License.Web.Services;

[Dependency(ServiceLifetime.Transient, ReplaceServices = true)]
[ExposeServices(typeof(IDeviceIdDomainService))]
public class SampleDeviceIdDomainService : DeviceIdDomainService
{
    #region Fields

    internal static readonly string FileToken = "example-device-token.txt";
    private readonly SampleDeviceIdFormatter _deviceIdFormatter;

    #endregion

    public SampleDeviceIdDomainService(SampleDeviceIdFormatter deviceIdFormatter)
    {
        _deviceIdFormatter = deviceIdFormatter;
        DeviceIdFormatter = deviceIdFormatter;
    }

    public override Task<string> GetDeviceIdAsync()
    {
        var deviceId = new DeviceIdBuilder()
            .AddMachineName()
            .AddFileToken(FileToken)
            .OnWindows(windows => windows
                .AddProcessorId()
                .AddMotherboardSerialNumber()
                .AddSystemUuid())
            .OnLinux(linux => linux
                .AddMotherboardSerialNumber()
                .AddSystemDriveSerialNumber())
            .OnMac(mac => mac
                .AddSystemDriveSerialNumber()
                .AddPlatformSerialNumber())
            .UseFormatter(_deviceIdFormatter)
            .ToString();

        return Task.FromResult(deviceId);
    }

    public override Task<bool> VerifyDeviceIdAsync(string deviceId)
    {
        // 52 Crockford characters + 1 checksum character
        var isValid = deviceId.Length == 53 && deviceId.All(c => Base32ByteArrayEncoder.CrockfordAlphabet.Contains(c));
        isValid = isValid && VerifyDeviceIdChecksum(deviceId);
        return Task.FromResult(isValid);
    }

    #region Private

    public bool VerifyDeviceIdChecksum(string encodedWithChecksum)
    {
        if (string.IsNullOrEmpty(encodedWithChecksum) || encodedWithChecksum.Length < 2)
        {
            return false;
        }

        // Separate the checksum character
        var encoded = encodedWithChecksum.Substring(0, encodedWithChecksum.Length - 1);
        var checksumChar = encodedWithChecksum[^1];

        // Recalculate the checksum
        var checksum = _deviceIdFormatter.ByteArrayEncoder.CalculateChecksum(encoded);
        var recalculatedChecksumChar = _deviceIdFormatter.Checksum[checksum];

        // Compare checksums
        return checksumChar == recalculatedChecksumChar;
    }

    #endregion
}