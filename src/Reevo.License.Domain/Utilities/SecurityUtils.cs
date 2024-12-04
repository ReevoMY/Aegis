using System.Security.Cryptography;
using Ardalis.GuardClauses;

namespace Reevo.License.Domain.Utilities;

public static class SecurityUtils
{

    /// <summary>
    ///     Calculates the SHA256 checksum of data.
    /// </summary>
    /// <param name="data">The data to calculate the checksum for.</param>
    /// <returns>The checksum as a base64 encoded string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if data is null.</exception>
    public static string CalculateChecksum(byte[] data)
    {
        Guard.Against.Null(data, nameof(data));

        var hash = SHA256.HashData(data);
        return Convert.ToBase64String(hash);
    }

    public static byte[] CalculateSha256Hash(byte[] data)
    {
        return SHA256.HashData(data);
    }

    /// <summary>
    ///     Verifies the checksum of data.
    /// </summary>
    /// <param name="data">The data to verify.</param>
    /// <param name="checksum">The checksum to verify against.</param>
    /// <returns>True if the checksum is valid, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown if data or checksum is null.</exception>
    public static bool VerifyChecksum(byte[] data, string checksum)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrEmpty(checksum);

        var calculatedChecksum = CalculateChecksum(data);
        return calculatedChecksum == checksum;
    }

    internal static byte[] GenerateAesKey()
    {
        using var aes = Aes.Create();
        aes.GenerateKey();
        return aes.Key;
    }

    internal static byte[] EncryptData(byte[] data, string passphrase)
    {
        Guard.Against.Null(data, nameof(data));
        Guard.Against.Null(passphrase, nameof(passphrase));

        // Derive a 32-byte key from the passphrase using PBKDF2 with SHA-256
        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        using var deriveBytes = new Rfc2898DeriveBytes(passphrase, salt, 10000, HashAlgorithmName.SHA256);
        var key = deriveBytes.GetBytes(32);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(data, 0, data.Length);
        }

        var encryptedData = ms.ToArray();
        return CombineByteArrays(salt, aes.IV, encryptedData);
    }

    internal static byte[] DecryptData(byte[] data, string passphrase)
    {
        Guard.Against.Null(data, nameof(data));
        Guard.Against.Null(passphrase, nameof(passphrase));

        var salt = new byte[16];
        var ivLength = Aes.Create().IV.Length;
        var iv = new byte[ivLength];
        var encryptedData = new byte[data.Length - salt.Length - ivLength];

        Array.Copy(data, 0, salt, 0, salt.Length);
        Array.Copy(data, salt.Length, iv, 0, ivLength);
        Array.Copy(data, salt.Length + ivLength, encryptedData, 0, encryptedData.Length);

        // Derive a 32-byte key from the passphrase using PBKDF2 with SHA-256
        using var deriveBytes = new Rfc2898DeriveBytes(passphrase, salt, 10000, HashAlgorithmName.SHA256);
        var key = deriveBytes.GetBytes(32);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(encryptedData);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var decryptedMs = new MemoryStream();
        cs.CopyTo(decryptedMs);

        return decryptedMs.ToArray();
    }

    /// <summary>
    ///     Signs data using RSA signature.
    /// </summary>
    /// <param name="data">The data to sign.</param>
    /// <param name="privateKey">The private key for signing.</param>
    /// <returns>The signature of the data.</returns>
    internal static byte[] SignData(byte[] data, string privateKey)
    {
        Guard.Against.Null(data, nameof(data));
        Guard.Against.Null(privateKey, nameof(privateKey));

        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);
        return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    /// <summary>
    ///     Verifies the signature of data using RSA signature.
    /// </summary>
    /// <param name="data">The data to verify.</param>
    /// <param name="signature">The signature to verify.</param>
    /// <param name="publicKey">The public key for verification.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    internal static bool VerifySignature(byte[] data, byte[] signature, string publicKey)
    {
        Guard.Against.Null(data, nameof(data));
        Guard.Against.Null(signature, nameof(signature));
        Guard.Against.Null(publicKey, nameof(publicKey));

        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
        return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }
    
    // Helper function to combine byte arrays
    private static byte[] CombineByteArrays(params byte[][] arrays)
    {
        var combinedLength = arrays.Sum(a => a.Length);
        var combined = new byte[combinedLength];
        var offset = 0;
        foreach (var array in arrays)
        {
            Array.Copy(array, 0, combined, offset, array.Length);
            offset += array.Length;
        }

        return combined;
    }
}