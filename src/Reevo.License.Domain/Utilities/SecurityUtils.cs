using System.Security.Cryptography;
using Ardalis.GuardClauses;

namespace Aegis.Utilities;

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

    internal static byte[] EncryptData(byte[] data, byte[] key)
    {
        Guard.Against.Null(data, nameof(data));
        Guard.Against.Null(key, nameof(key));

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
        return CombineByteArrays(aes.IV, encryptedData);
    }

    internal static byte[] DecryptData(byte[] data, byte[] key)
    {
        Guard.Against.Null(data, nameof(data));
        Guard.Against.Null(key, nameof(key));
        
        var ivLength = Aes.Create().IV.Length;
        var iv = new byte[ivLength];
        var encryptedData = new byte[data.Length - ivLength];

        Array.Copy(data, 0, iv, 0, ivLength);
        Array.Copy(data, ivLength, encryptedData, 0, data.Length - ivLength);

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
    private static byte[] CombineByteArrays(byte[] array1, byte[] array2)
    {
        var combined = new byte[array1.Length + array2.Length];
        Array.Copy(array1, 0, combined, 0, array1.Length);
        Array.Copy(array2, 0, combined, array1.Length, array2.Length);
        return combined;
    }
}