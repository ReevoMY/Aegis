using DeviceId;
using DeviceId.Encoders;
using System.Numerics;
using System.Security.Cryptography;

namespace Reevo.License.Domain.Utilities;

public static class HardwareUtils
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    private const string ChecksumAlphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ*$=U";

    /// <summary>
    ///     Gets a unique hardware identifier for the current machine.
    /// </summary>
    /// <returns>A string representing the hardware identifier.</returns>
    public static string GetHardwareId()
    {
        //var deviceId = new DeviceIdBuilder()
        //    .AddMachineName()
        //    .AddOsVersion()
        //    .AddFileToken("example-device-token.txt")
        //    .ToString();

        //return deviceId;

        // Test area
        var test = new DeviceIdBuilder()
            .AddMachineName()
            .AddOsVersion()
            .AddFileToken("example-device-token.txt");

        var formatter = DeviceIdFormatters.DefaultV6;
        var test2 = formatter.GetDeviceId(test.Components);

        var deviceId = test.ToString();

        // Test3
        // Convert input to a BigInteger
        var number = BigInteger.Parse("123456789ABCDEF", System.Globalization.NumberStyles.HexNumber);

        // Encode the number to Base32
        var encoded = EncodeBase32(number);

        // Calculate the checksum
        var checksum = (int)(number % 37);
        var checksumChar = ChecksumAlphabet[checksum];

        // Append the checksum character
        var test3 =  encoded + checksumChar;

        // Test4
        var isValid = deviceId.Length == 52 && deviceId.All(c => Base32ByteArrayEncoder.CrockfordAlphabet.Contains(c));

        return deviceId;
    }

    /// <summary>
    ///     Validates a hardware identifier against the current machine's hardware identifier.
    /// </summary>
    /// <param name="hardwareId">The hardware identifier to validate.</param>
    /// <returns>True if the hardware identifier matches, false otherwise.</returns>
    public static bool ValidateHardwareId(string hardwareId)
    {
        return GetHardwareId() == hardwareId;
    }

    // Test zone
    private static string EncodeBase32(BigInteger number)
    {
        var result = string.Empty;
        while (number > 0)
        {
            int remainder = (int)(number % 32);
            result = Alphabet[remainder] + result;
            number /= 32;
        }
        return result;
    }
}