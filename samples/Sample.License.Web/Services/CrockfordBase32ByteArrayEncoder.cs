using DeviceId;
using DeviceId.Encoders;
using System.Numerics;
using System.Text;
using Volo.Abp.DependencyInjection;

namespace Reevo.License.Domain.Service;

[Dependency(ServiceLifetime.Transient, ReplaceServices = true)]
[ExposeServices(typeof(IByteArrayEncoder))]
public class CrockfordBase32ByteArrayEncoder : IByteArrayEncoder
{
    #region Fields

    /// <inheritdoc cref="Base32ByteArrayEncoder.CrockfordAlphabet"/>
    public static string CrockfordAlphabet => Base32ByteArrayEncoder.CrockfordAlphabet;

    /// <summary>
    /// Gets the checksum for verification.
    /// </summary>
    private readonly string? _checksum;

    #endregion

    #region ctor

    public CrockfordBase32ByteArrayEncoder()
    {
    }

    public CrockfordBase32ByteArrayEncoder(string checksum)
    {
        _checksum = checksum;
    }

    #endregion

    public virtual string Encode(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        if (bytes.Length == 0)
        {
            return string.Empty;
        }

        const int shift = 5;
        const int mask = 31;

        var outputLength = (bytes.Length * 8 + shift - 1) / shift;
        var sb = new StringBuilder(outputLength);

        var offset = 0;
        var last = bytes.Length;
        int buffer = bytes[offset++];
        var bitsLeft = 8;
        while (bitsLeft > 0 || offset < last)
        {
            if (bitsLeft < shift)
            {
                if (offset < last)
                {
                    buffer <<= 8;
                    buffer |= bytes[offset++] & 0xff;
                    bitsLeft += 8;
                }
                else
                {
                    var pad = shift - bitsLeft;
                    buffer <<= pad;
                    bitsLeft += pad;
                }
            }

            var index = mask & (buffer >> (bitsLeft - shift));
            bitsLeft -= shift;
            sb.Append(CrockfordAlphabet[index]);
        }

        if (_checksum != null)
        {
            // Calculate the checksum and append it to the encoded string
            var encodedString = sb.ToString();
            var checksum = CalculateChecksum(encodedString);
            sb.Append(_checksum[checksum]);
        }

        return sb.ToString();
    }

    #region Private

    internal int CalculateChecksum(string encodedString)
    {
        BigInteger number = 0;
        foreach (var c in encodedString)
        {
            number = number * 32 + CrockfordAlphabet.IndexOf(c);
        }
        return (int)(number % 37);
    }

    #endregion
}