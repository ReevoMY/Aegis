using System.Reflection;
using System.Text.Json;
using Reevo.License.Domain;
using Reevo.License.Domain.Shared.Enum;
using Reevo.License.Domain.Exceptions;
using Reevo.License.Domain.Models;
using Reevo.License.Domain.Models.Utils;
using Reevo.License.Domain.Utilities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Reevo.License.Domain.Tests;

public class LicenseManagerTests
{
    #region Fields

    private readonly LicensingSecrets _licenseKeys;

    #endregion

    public LicenseManagerTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(GetType().Assembly)
            .Build();
        _licenseKeys = new LicensingSecrets()
        {
            PublicKey = configuration.GetSection("LicensingSecrets:PublicKey").Value!,
            PrivateKey = configuration.GetSection("LicensingSecrets:PrivateKey").Value!,
            ApiKey = configuration.GetSection("LicensingSecrets:ApiKey").Value!
        };
    }

    #region SaveLicense

    [Fact]
    public void SaveLicense_SavesLicenseToFileCorrectly()
    {
        // Arrange
        var license = GenerateLicense();
        var filePath = Path.GetTempFileName(); // Use a temporary file

        // Act
        LicenseManager.SaveLicense(license, filePath);

        // Assert
        File.Exists(filePath).Should().BeTrue();

        // Clean up
        File.Delete(filePath);
    }

    [Fact]
    public void SaveLicense_ThrowsExceptionForNullLicense()
    {
        // Arrange
        var filePath = Path.GetTempFileName();

        // Act
        var act = () => LicenseManager.SaveLicense<BaseLicense>(null!, null, filePath);

        // Assert
        act.Should().Throw<ArgumentNullException>();

        // Clean up
        File.Delete(filePath);
    }

    [Fact]
    public void SaveLicense_ThrowsExceptionForEmptyFilePath()
    {
        // Arrange
        var license = GenerateLicense();

        // Act
        var act = () => LicenseManager.SaveLicense(license, "");

        // Assert
        act.Should().Throw<DirectoryNotFoundException>();
    }

    [Fact]
    public void SaveLicense_ThrowsExceptionForInvalidFilePath()
    {
        // Arrange
        var license = GenerateLicense();
        const string filePath = "Invalid/File/Path"; // This should be an invalid path on most systems

        // Act
        var act = () => LicenseManager.SaveLicense(license, filePath);

        // Assert
        act.Should().Throw<DirectoryNotFoundException>();
    }

    #endregion

    #region LoadLicenseAsync

    [Fact]
    public async Task LoadLicenseAsync_LoadsLicenseFromFileCorrectly()
    {
        foreach (var licenseType in EnumsNET.Enums.GetValues<LicenseType>())
        {
            // Arrange
            var license = GenerateLicense(licenseType);
            var filePath = Path.GetTempFileName();
            LicenseManager.SaveLicense(license, filePath, _licenseKeys.PrivateKey);

            // Act
            var loadedLicense = await LicenseManager.LoadLicenseAsync(filePath);

            // Assert
            loadedLicense.Should().NotBeNull();
            loadedLicense!.LicenseKey.Should().Be(license.LicenseKey);
            loadedLicense.Type.Should().Be(license.Type);

            // Clean up
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task LoadLicenseAsync_ThrowsExceptionForInvalidLicenseFile()
    {
        // Arrange
        var filePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(filePath, "Invalid License Data"); // Corrupt the file

        // Act
        var act = async () => await LicenseManager.LoadLicenseAsync(filePath);

        // Assert
        await act.Should().ThrowAsync<InvalidLicenseFormatException>()
            .WithMessage("Invalid license file format.");

        // Clean up
        File.Delete(filePath);
    }

    [Fact]
    public async Task LoadLicenseAsync_ThrowsExceptionForNullFilePath()
    {
        // Act
        var act = async () => await LicenseManager.LoadLicenseAsync(null!, ActivationMode.Offline);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task LoadLicenseAsync_ThrowsExceptionForEmptyFilePath()
    {
        // Act
        var act = async () => await LicenseManager.LoadLicenseAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task LoadLicenseAsync_ThrowsExceptionForInvalidFilePath()
    {
        // Arrange
        const string filePath = "Invalid/File/Path"; // This should be an invalid path

        // Act
        var act = async () => await LicenseManager.LoadLicenseAsync(filePath);

        // Assert
        await act.Should().ThrowAsync<DirectoryNotFoundException>();
    }

    #endregion

    #region IsFeatureEnabled

    [Fact]
    public void IsFeatureEnabled_ReturnsCorrectValue()
    {
        foreach (var licenseType in EnumsNET.Enums.GetValues<LicenseType>())
        {

            // Arrange
            var license = GenerateLicense(licenseType);
            license.Features.Add("Feature1", true);
            SetLicense(license);

            // Act
            var isEnabled = LicenseManager.IsFeatureEnabled("Feature1");

            // Assert
            isEnabled.Should().BeTrue();
        }
    }

    [Fact]
    public void IsFeatureEnabled_ReturnsFalseForNonExistingFeature()
    {
        // Arrange
        var license = GenerateLicense();
        SetLicense(license);

        // Act
        var isEnabled = LicenseManager.IsFeatureEnabled("NonExistingFeature");

        // Assert
        isEnabled.Should().BeFalse();
    }

    [Fact]
    public void IsFeatureEnabled_ReturnsFalseForDisabledFeature()
    {
        // Arrange
        var license = GenerateLicense();
        license.Features.Add("Feature1", false);
        SetLicense(license);

        // Act
        var isEnabled = LicenseManager.IsFeatureEnabled("Feature1");

        // Assert
        isEnabled.Should().BeFalse();
    }

    #endregion

    #region ThrowIfNotAllowed

    [Fact]
    public void ThrowIfNotAllowed_ThrowsExceptionForDisabledFeature()
    {
        // Arrange
        var license = GenerateLicense();
        license.Features.Add("Feature1", false);
        SetLicense(license);

        // Act
        var act = () => LicenseManager.ThrowIfNotAllowed("Feature1");

        // Assert
        act.Should().Throw<FeatureNotLicensedException>();
    }

    [Fact]
    public void ThrowIfNotAllowed_DoesNotThrowExceptionForEnabledFeature()
    {
        // Arrange
        var license = GenerateLicense();
        license.Features.Add("Feature1", true);
        SetLicense(license);

        // Act & Assert (no exception should be thrown)
        LicenseManager.ThrowIfNotAllowed("Feature1");
    }

    #endregion

    #region SetServerBaseEndpoint

    [Theory]
    [InlineData("https://api-endpoint.com")]
    [InlineData("https://api-endpoint.com/")]
    public void SetServerBaseEndpoint_SetsEndpointCorrectly(string newEndpoint)
    {
        // Arrange
        var expectedEndpoint = newEndpoint.EndsWith("/") ? newEndpoint[..^1] : newEndpoint; // Remove trailing slash

        // Act
        LicenseManager.SetServerBaseEndpoint(newEndpoint);

        // Assert
        var serverBaseEndpointField = typeof(LicenseManager).GetField("_serverBaseEndpoint", BindingFlags.NonPublic | BindingFlags.Static);
        var serverBaseEndpointValue = serverBaseEndpointField!.GetValue(null);
        serverBaseEndpointValue.Should().Be(expectedEndpoint);
    }

    [Fact]
    public void SetServerBaseEndpoint_ThrowsExceptionForNullEndpoint()
    {
        // Act
        var act = () => LicenseManager.SetServerBaseEndpoint(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SetServerBaseEndpoint_ThrowsExceptionForEmptyEndpoint()
    {
        // Act
        var act = () => LicenseManager.SetServerBaseEndpoint("");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region SetHeartbeatInterval

    [Fact]
    public void SetHeartbeatInterval_SetsIntervalCorrectly()
    {
        // Arrange
        var newInterval = TimeSpan.FromMinutes(15);

        // Act
        LicenseManager.SetHeartbeatInterval(newInterval);

        // Assert
        var heartbeatIntervalField = typeof(LicenseManager).GetField("_heartbeatInterval",
            BindingFlags.NonPublic | BindingFlags.Static);
        var heartbeatIntervalValue = heartbeatIntervalField!.GetValue(null);
        heartbeatIntervalValue.Should().Be(newInterval);
    }

    [Fact]
    public void SetHeartbeatInterval_ThrowsExceptionForNegativeInterval()
    {
        // Act
        var act = () => LicenseManager.SetHeartbeatInterval(TimeSpan.FromMinutes(-1));

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region VerifyChecksum

    [Fact]
    public void VerifyChecksum_ReturnsTrueForMatchingChecksum()
    {
        // Arrange
        var license = GenerateLicense();
        var licenseData = JsonSerializer.SerializeToUtf8Bytes(license);
        var checksum = SecurityUtils.CalculateChecksum(licenseData);

        // Act
        var isValid = SecurityUtils.VerifyChecksum(licenseData, checksum);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyChecksum_ReturnsFalseForMismatchedChecksum()
    {
        // Arrange
        var license = GenerateLicense();
        var licenseData = JsonSerializer.SerializeToUtf8Bytes(license);
        const string incorrectChecksum = "invalid-checksum"; // Incorrect checksum

        // Act
        var isValid = SecurityUtils.VerifyChecksum(licenseData, incorrectChecksum);

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region VerifySignature

    [Fact]
    public void VerifySignature_ReturnsTrueForValidSignature()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateLicense();
        var licenseData = LicenseManager.SaveLicense(license); // Encrypted and signed
        var publicKey = LicenseUtils.GetLicensingSecrets().PublicKey; // Get the public key

        // Act
        var (hash, signature, _, _) = LicenseValidator.SplitLicenseData(licenseData);
        var isValid = SecurityUtils.VerifySignature(hash, signature, publicKey); // Verify signature of hash

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifySignature_ReturnsFalseForInvalidSignature()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateLicense();
        var licenseData = LicenseManager.SaveLicense(license); // Encrypted and signed
        var publicKey = LicenseUtils.GetLicensingSecrets().PublicKey; // Get the public key
        var invalidSignature = new byte[16]; // Create a fake, invalid signature

        // Act
        var (hash, _, _, _) = LicenseValidator.SplitLicenseData(licenseData);

        var isValid = SecurityUtils.VerifySignature(hash, invalidSignature, publicKey);

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region Private

    // Helper method to generate a license for testing
    private BaseLicense GenerateLicense(LicenseType type = LicenseType.Standard)
    {
        return type switch
        {
            LicenseType.Standard => LicenseGenerator.GenerateStandardLicense("TestUser"),
            LicenseType.Trial => LicenseGenerator.GenerateTrialLicense(TimeSpan.FromDays(7)),
            LicenseType.NodeLocked => LicenseGenerator.GenerateNodeLockedLicense(HardwareUtils.GetHardwareId()),
            LicenseType.Subscription => LicenseGenerator.GenerateSubscriptionLicense("TestUser", TimeSpan.FromDays(30)),
            LicenseType.Concurrent => LicenseGenerator.GenerateConcurrentLicense("TestUser", 6),
            LicenseType.Floating => LicenseGenerator.GenerateFloatingLicense("TestUser", 5),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private void LoadSecretKeys()
    {
        var secretPath = Path.GetTempFileName();
        LicenseUtils.GenerateLicensingSecrets("MySecretTestKey", secretPath, "12345678-90ab-cdef-ghij-klmnopqrst");
        LicenseUtils.LoadLicensingSecrets("MySecretTestKey", secretPath);
    }

    private static void SetLicense(BaseLicense license)
    {
        var currentProperty = typeof(LicenseManager).GetProperty("Current", BindingFlags.Public | BindingFlags.Static);
        currentProperty!.SetValue(null, license);
    }
    
    #endregion
}