using System.Security.Cryptography;
using DeviceId;
using DeviceId.Encoders;
using DeviceId.Formatters;

namespace Reevo.License.Domain.Service;

/// <inheritdoc cref="DeviceId.DeviceIdFormatters"/>
public static class DeviceIdFormatters
{
    public static IDeviceIdFormatter DefaultV5 => DeviceId.DeviceIdFormatters.DefaultV5;

    public static IDeviceIdFormatter DefaultV6 => DeviceId.DeviceIdFormatters.DefaultV6;

    /// <summary>
    /// Just an example of how to create a custom <see cref="IDeviceIdFormatter"/>.
    /// </summary>
    public static IDeviceIdFormatter CrockfordBaseDeviceIdFormatter => new HashDeviceIdFormatter(new ByteArrayHasher(SHA256.Create), new CrockfordBase32ByteArrayEncoder());
}