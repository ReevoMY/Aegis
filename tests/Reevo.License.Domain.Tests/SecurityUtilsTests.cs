using System.Security.Cryptography;
using Aegis.Utilities;
using FluentAssertions;

namespace Aegis.Tests;

public class SecurityUtilsTests
{
    [Fact]
    public void GenerateAesKey_ReturnsKeyOfCorrectSize()
    {
        // Act
        var key = SecurityUtils.GenerateAesKey();

        // Assert
        key.Length.Should().Be(32); // AES-256 key size is 32 bytes
    }

    [Fact]
    public void EncryptData_DecryptsDataCorrectly()
    {
        // Arrange
        var key = SecurityUtils.GenerateAesKey();
        var testData = GenerateTestData(512);

        // Act
        var encryptedData = SecurityUtils.EncryptData(testData, key);
        var decryptedData = SecurityUtils.DecryptData(encryptedData, key);

        // Assert
        encryptedData.Should().NotEqual(testData);
        decryptedData.Should().Equal(testData);
        decryptedData.Length.Should().Be(512);
    }

    [Fact]
    public void EncryptData_ThrowsException_ForNullData()
    {
        // Arrange
        var key = SecurityUtils.GenerateAesKey();

        // Act & Assert
        var act = () => SecurityUtils.EncryptData(null!, key);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EncryptData_ThrowsException_ForNullKey()
    {
        // Arrange
        var testData = GenerateTestData(64);

        // Act & Assert
        var act = () => SecurityUtils.EncryptData(testData, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DecryptData_ThrowsException_ForNullData()
    {
        // Arrange
        var key = SecurityUtils.GenerateAesKey();

        // Act & Assert
        var act = () => SecurityUtils.DecryptData(null!, key);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DecryptData_ThrowsException_ForNullKey()
    {
        // Arrange
        var testData = GenerateTestData(64);

        // Act & Assert
        var act = () => SecurityUtils.DecryptData(testData, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SignData_VerifiesSignatureCorrectly()
    {
        // Arrange
        var rsa = RSA.Create();
        var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
        var testData = GenerateTestData(256);

        // Act
        var signature = SecurityUtils.SignData(testData, privateKey);
        var isValid = SecurityUtils.VerifySignature(testData, signature, publicKey);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void SignData_ThrowsException_ForNullData()
    {
        // Arrange
        var rsa = RSA.Create();
        var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());

        // Act & Assert
        var act = () => SecurityUtils.SignData(null!, privateKey);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SignData_ThrowsException_ForNullPrivateKey()
    {
        // Arrange
        var testData = GenerateTestData(256);

        // Act & Assert
        var act = () => SecurityUtils.SignData(testData, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VerifySignature_ThrowsException_ForNullData()
    {
        // Arrange
        var rsa = RSA.Create();
        var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
        var signature = GenerateTestData(128);

        // Act & Assert
        var act = () => SecurityUtils.VerifySignature(null!, signature, publicKey);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VerifySignature_ThrowsException_ForNullSignature()
    {
        // Arrange
        var rsa = RSA.Create();
        var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
        var testData = GenerateTestData(256);

        // Act & Assert
        var act = () => SecurityUtils.VerifySignature(testData, null!, publicKey);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VerifySignature_ThrowsException_ForNullPublicKey()
    {
        // Arrange
        var testData = GenerateTestData(256);
        var signature = GenerateTestData(128);

        // Act & Assert
        var act = () => SecurityUtils.VerifySignature(testData, signature, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CalculateChecksum_ReturnsCorrectChecksum()
    {
        // Arrange
        var testData = GenerateTestData(256);

        // Act
        var checksum = SecurityUtils.CalculateChecksum(testData);

        // Assert
        checksum.Should().NotBeNullOrEmpty();
        var hash = Convert.FromBase64String(checksum);
        hash.Length.Should().Be(32); // SHA256 hash size is 32 bytes
    }

    [Fact]
    public void CalculateChecksum_ThrowsException_ForNullData()
    {
        // Act & Assert
        var act = () => SecurityUtils.CalculateChecksum(null!);
        act.Should().Throw<ArgumentNullException>();
    }
    
    [Fact]
    public void CalculateSha256Hash_ReturnsCorrectHash()
    {
        // Arrange
        var testData = GenerateTestData(256);

        // Act
        var hash = SecurityUtils.CalculateSha256Hash(testData);

        // Assert
        hash.Length.Should().Be(32); // SHA256 hash size is 32 bytes
    }

    [Fact]
    public void CalculateSha256Hash_ThrowsException_ForNullData()
    {
        // Act & Assert
        var act = () => SecurityUtils.CalculateSha256Hash(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VerifyChecksum_ReturnsTrue_ForMatchingChecksum()
    {
        // Arrange
        var testData = GenerateTestData(256);
        var checksum = SecurityUtils.CalculateChecksum(testData);

        // Act
        var isValid = SecurityUtils.VerifyChecksum(testData, checksum);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyChecksum_ReturnsFalse_ForMismatchedChecksum()
    {
        // Arrange
        var testData = GenerateTestData(256);
        var checksum2 = SecurityUtils.CalculateChecksum(GenerateTestData(256)); // Different data

        // Act
        var isValid = SecurityUtils.VerifyChecksum(testData, checksum2);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void VerifyChecksum_ThrowsException_ForNullData()
    {
        // Arrange
        var checksum = SecurityUtils.CalculateChecksum(GenerateTestData(256));

        // Act & Assert
        var act = () => SecurityUtils.VerifyChecksum(null!, checksum);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VerifyChecksum_ThrowsException_ForNullChecksum()
    {
        // Arrange
        var testData = GenerateTestData(256);

        // Act & Assert
        var act = () => SecurityUtils.VerifyChecksum(testData, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #region Private

    // Helper method to generate test data
    private static byte[] GenerateTestData(int length)
    {
        var data = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(data);
        return data;
    }

    #endregion
}