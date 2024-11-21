using System.Security.Cryptography;
using System.Text;
using Ardalis.GuardClauses;
using DeviceId;
using DeviceId.Encoders;
using Reevo.License.Domain.Service;
using Volo.Abp.DependencyInjection;

namespace Sample.License.Web.Services;

/// <summary>
/// An example implementation of <see cref="IDeviceIdFormatter"/> that combines the components into a hash.
/// </summary>
[Dependency(ServiceLifetime.Transient, ReplaceServices = true)]
//[ExposeServices(typeof(IDeviceIdFormatter))]
public class SampleDeviceIdFormatter : IDeviceIdFormatter
{
    #region Fields

    internal readonly string Checksum = "0123456789ABCDEFGHJKMNPQRSTVWXYZ*$=U";

    /// <summary>
    /// The <see cref="IByteArrayHasher"/> to use to hash the device ID.
    /// </summary>
    internal readonly IByteArrayHasher ByteArrayHasher;

    /// <summary>
    /// The sample <see cref="IByteArrayEncoder"/> implementation to use to encode the resulting hash.
    /// </summary>
    /// <remarks>The checksum can be swapped with any order of unique alphabets.</remarks>
    internal readonly CrockfordBase32ByteArrayEncoder ByteArrayEncoder;

    public SampleDeviceIdFormatter()
    {
        ByteArrayHasher = new ByteArrayHasher(SHA256.Create);
        ByteArrayEncoder = new CrockfordBase32ByteArrayEncoder(Checksum);
    }

    #endregion

    public string GetDeviceId(IDictionary<string, IDeviceIdComponent> components)
    {
        Guard.Against.NullOrEmpty(
            components, 
            exceptionCreator: () => new InvalidOperationException($"Please provide the {nameof(IDeviceIdComponent)} components."));

        var value = string.Join(",", components.OrderBy(x => x.Key).Select(x => x.Value.GetValue()).ToArray());
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = ByteArrayHasher.Hash(bytes);

        return ByteArrayEncoder.Encode(hash);
    }
}