using Aegis.Exceptions;
using Aegis.Models.Utils;
using Aegis.Utilities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Reevo.License.Domain.Tests;

public class LicenseUtilsTests
{
    #region Fields

    private readonly IConfigurationRoot _configuration;
    private readonly string _signKey = "MySecretTestKey";
    private readonly string _publicKey;
    private readonly string _privateKey;
    private readonly string _apiKey;

    #endregion

    public LicenseUtilsTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddUserSecrets(GetType().Assembly)
            .Build();

        _publicKey = _configuration.GetSection("LicensingSecrets:PublicKey").Value!;
        _privateKey = _configuration.GetSection("LicensingSecrets:PrivateKey").Value!;
        _apiKey = _configuration.GetSection("LicensingSecrets:ApiKey").Value!;
    }

    [Fact]
    public void GetLicensingSecrets_LoadKeysCorrectly()
    {
        // Act
        var keys = LicenseUtils.GetLicensingSecrets();

        // Assert
        keys.Should().NotBeNull();
        keys.PublicKey.Should().Be(_publicKey);
        keys.PrivateKey.Should().Be(_privateKey);
        keys.ApiKey.Should().Be(_apiKey);
    }

    [Fact]
    public void LoadLicensingSecrets_LoadsKeysFromConfigurationSection()
    {
        // Arrange
        var section = _configuration.GetSection("LicensingSecrets");

        // Act
        var keys = LicenseUtils.LoadLicensingSecrets(section);

        // Assert
        keys.PublicKey.Should().Be(_publicKey);
        keys.PrivateKey.Should().Be(_privateKey);
        keys.ApiKey.Should().Be(_apiKey);
    }

    [Fact]
    public void LoadLicensingSecrets_ThrowsException_ForInvalidPath()
    {
        // Arrange
        const string invalidPath = "Invalid/Path";

        // Act
        var act = () => LicenseUtils.LoadLicensingSecrets(_signKey, invalidPath);

        // Assert
        act.Should().Throw<KeyManagementException>()
            .WithMessage("Failed to load license signature keys.");
    }

    [Fact]
    public void LoadLicensingSecrets_ThrowsException_ForInvalidData()
    {
        // Arrange
        var filePath = Path.GetTempFileName();
        File.WriteAllText(filePath, "Invalid JSON Data"); // Write invalid JSON

        // Act
        var act = () => LicenseUtils.LoadLicensingSecrets(_signKey, filePath);
        
        // Assert
        act.Should().Throw<KeyManagementException>()
            .WithMessage("Failed to load license signature keys.");

        // Clean up
        File.Delete(filePath);
    }

    [Fact]
    public void LoadLicensingSecrets_LoadsKeysFromFileCorrectly()
    {
        // Arrange
        var keys = GenerateLicensingSecrets(_signKey, out var filePath);

        // Act
        var loadedKeys = LicenseUtils.LoadLicensingSecrets(_signKey, filePath);

        // Assert
        loadedKeys.PublicKey.Should().Be(keys.PublicKey);
        loadedKeys.PrivateKey.Should().Be(keys.PrivateKey);
        loadedKeys.ApiKey.Should().Be(keys.ApiKey);

        // Clean up
        File.Delete(filePath);
    }

    [Fact]
    public void GenerateLicensingSecrets_GeneratesAndSavesKeysCorrectly()
    {
        // Arrange & Act
        var keys = GenerateLicensingSecrets(_signKey, out var filePath);

        // Assert
        keys.Should().NotBeNull();
        keys.PublicKey.Should().NotBeNullOrEmpty();
        keys.PrivateKey.Should().NotBeNullOrEmpty();
        keys.ApiKey.Should().NotBeNullOrEmpty();
        File.Exists(filePath).Should().BeTrue();

        var content = File.ReadAllBytes(filePath);
        content.Length.Should().BeGreaterThan(0);

        // Clean up
        File.Delete(filePath);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Invalid/Path")]
    public void GenerateLicensingSecrets_ThrowsException_ForInvalidPath(string invalidPath)
    {
        // Act
        var act = () => LicenseUtils.GenerateLicensingSecrets(_signKey, invalidPath, _apiKey);

        // Assert
        act.Should().Throw<KeyManagementException>()
            .WithMessage("Failed to generate and save license signature keys.");
    }

    #region Private

    /// <summary>
    /// Helper method to generate keys and save them to a file
    /// </summary>
    private LicensingSecrets GenerateLicensingSecrets(string key, out string filePath, string? overriddenFilePath = null)
    {
        filePath = !string.IsNullOrEmpty(overriddenFilePath) ? overriddenFilePath : Path.GetTempFileName();
        var keys = LicenseUtils.GenerateLicensingSecrets(key, filePath, _apiKey);
        return keys;
    }

    #endregion
}