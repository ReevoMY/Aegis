using Aegis.Enums;
using Aegis.Exceptions;
using Aegis.Models;
using Aegis.Utilities;
using FluentAssertions;

namespace Aegis.Tests;

public class LicenseBuilderTests
{
    [Fact]
    public void WithExpiryDate_SetsExpirationDateCorrectly_ForStandardLicense()
    {
        // Arrange
        var baseLicense = CreateBaseLicense();
        var expectedExpiryDate = DateTime.UtcNow.AddDays(10).Date;

        // Act
        var license = baseLicense.WithExpiryDate(expectedExpiryDate);

        // Assert
        license.ExpirationDate!.Value.Date.Should().Be(expectedExpiryDate);
    }

    [Fact]
    public void WithExpiryDate_ThrowsExceptionForTrialLicense()
    {
        // Arrange
        var baseLicense = CreateBaseLicense(LicenseType.Trial);
        var expiryDate = DateTime.UtcNow.AddDays(10);

        // Act & Assert
        FluentActions.Invoking(() => baseLicense.WithExpiryDate(expiryDate)).Should()
            .Throw<LicenseGenerationException>();
    }

    [Fact]
    public void WithFeature_AddsNewFeatureCorrectly()
    {
        // Arrange
        var baseLicense = CreateBaseLicense();

        // Act
        var license = baseLicense.WithFeature("TestFeature", true);

        // Assert
        license.Features.ContainsKey("TestFeature").Should().BeTrue();
        license.Features["TestFeature"].Should().BeTrue();
    }

    [Fact]
    public void WithFeature_UpdatesExistingFeatureCorrectly()
    {
        // Arrange
        var baseLicense = CreateBaseLicense();
        baseLicense.Features.Add("TestFeature", false);

        // Act
        var license = baseLicense.WithFeature("TestFeature", true);

        // Assert
        license.Features.ContainsKey("TestFeature").Should().BeTrue();
        license.Features["TestFeature"].Should().BeTrue();
    }

    [Fact]
    public void WithFeatures_SetsFeaturesCorrectly()
    {
        // Arrange
        var baseLicense = CreateBaseLicense();
        var features = new Dictionary<string, bool>
        {
            { "Feature1", true },
            { "Feature2", false }
        };

        // Act
        var license = baseLicense.WithFeatures(features);

        // Assert
        license.Features.Should().BeEquivalentTo(features);
    }

    [Fact]
    public void WithIssuer_SetsIssuerCorrectly()
    {
        // Arrange
        var baseLicense = CreateBaseLicense();
        const string issuer = "Aegis Software Inc.";

        // Act
        var license = baseLicense.WithIssuer(issuer);

        // Assert
        license.Issuer.Should().Be(issuer);
    }

    [Fact]
    public void WithLicenseKey_SetsLicenseKeyCorrectly()
    {
        // Arrange
        var baseLicense = CreateBaseLicense();
        const string licenseKey = "ABCD-EFGH-IJKL-MNOP";

        // Act
        var license = baseLicense.WithLicenseKey(licenseKey);

        // Assert
        license.LicenseKey.Should().Be(licenseKey);
    }

    [Fact]
    public async Task SaveLicense_CallsLicenseManagerMethodsWithCorrectArguments()
    {
        // Arrange
        var baseLicense = CreateBaseLicense();
        var filePath = $@"{Path.GetTempPath()}\license.lic";
        var secretPath = Path.GetTempFileName();
        LicenseUtils.GenerateLicensingSecrets("MySecretTestKey", secretPath, "12345678-90ab-cdef-ghij-klmnopqrst");
        LicenseUtils.LoadLicensingSecrets("MySecretTestKey", secretPath);

        // Act 
        baseLicense.SaveLicense(filePath);

        // Assert
        var licenseData = await File.ReadAllBytesAsync(filePath);
        licenseData.Should().NotBeNullOrEmpty();

        var license = await LicenseManager.LoadLicenseAsync(licenseData);
        license.Should().NotBeNull();
        license!.Type.Should().Be(baseLicense.Type);
        license.Issuer.Should().Be(baseLicense.Issuer);
        license.LicenseId.Should().Be(baseLicense.LicenseId);
        license.LicenseKey.Should().Be(baseLicense.LicenseKey);
        license.Features.Should().BeEquivalentTo(baseLicense.Features);
        license.ExpirationDate!.Value.Date.Should().Be(baseLicense.ExpirationDate!.Value.Date);
        license.IssuedOn.Date.Should().Be(baseLicense.IssuedOn.Date);

        // Clean up
        File.Delete(filePath);
    }

    #region Private

    private BaseLicense CreateBaseLicense(LicenseType type = LicenseType.Standard)
    {
        BaseLicense license = type switch
        {
            LicenseType.Standard => new StandardLicense("TestUser"),
            LicenseType.Trial => new TrialLicense(TimeSpan.FromDays(7)),
            LicenseType.NodeLocked => new NodeLockedLicense("TestHardwareId"),
            LicenseType.Subscription => new SubscriptionLicense("TestUser", TimeSpan.FromDays(30)),
            LicenseType.Floating => new FloatingLicense("TestUser", 5),
            LicenseType.Concurrent => new ConcurrentLicense("TestUser", 5),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        if (license.Type != LicenseType.Trial)
            license.WithExpiryDate(DateTime.UtcNow.AddDays(10));

        return license.WithIssuer("Aegis Software").WithFeatures(new Dictionary<string, bool>
        {
            { "Feature1", true }, { "Feature2", false }
        });
    }

    #endregion
}