﻿using System.Reflection;
using Reevo.License.Domain;
using Reevo.License.Domain.Models;
using Reevo.License.Domain.Utilities;
using FluentAssertions;

namespace Reevo.License.Domain.Tests;

public class LicenseValidatorTests
{
    [Fact]
    public void ValidateStandardLicense_ReturnsTrue_ForValidLicense()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateStandardLicense();
        var licenseData = LicenseManager.SaveLicense(license);

        // Act
        var isValid = LicenseValidator.ValidateStandardLicense(licenseData, license.UserName, license.LicenseKey);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateStandardLicense_ReturnsFalse_ForIncorrectUserName()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateStandardLicense();
        var licenseData = LicenseManager.SaveLicense(license);
        const string incorrectUserName = "IncorrectUser";

        // Act
        var isValid = LicenseValidator.ValidateStandardLicense(licenseData, incorrectUserName, license.LicenseKey);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateStandardLicense_ReturnsFalse_ForIncorrectSerialNumber()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateStandardLicense();
        var licenseData = LicenseManager.SaveLicense(license);
        const string incorrectSerialNumber = "IncorrectSerialNumber";

        // Act
        var isValid = LicenseValidator.ValidateStandardLicense(licenseData, license.UserName, incorrectSerialNumber);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateStandardLicense_ReturnsFalse_ForExpiredLicense()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateStandardLicense();
        license.ExpirationDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1));
        var licenseData = LicenseManager.SaveLicense(license);

        // Act
        var isValid = LicenseValidator.ValidateStandardLicense(licenseData, license.UserName, license.LicenseKey);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateTrialLicense_ReturnsTrue_ForValidLicense()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateTrialLicense(TimeSpan.FromDays(7));
        var licenseData = LicenseManager.SaveLicense(license);

        // Act
        var isValid = LicenseValidator.ValidateTrialLicense(licenseData);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateTrialLicense_ReturnsFalse_ForExpiredLicense()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateTrialLicense(TimeSpan.FromDays(7));
        license.ExpirationDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1)); 
        var licenseData = LicenseManager.SaveLicense(license); 

        // Act
        var isValid = LicenseValidator.ValidateTrialLicense(licenseData);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateTrialLicense_ReturnsFalse_ForZeroTrialPeriod()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateTrialLicense(TimeSpan.Zero);
        var licenseData = LicenseManager.SaveLicense(license);

        // Act
        var isValid = LicenseValidator.ValidateTrialLicense(licenseData);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateNodeLockedLicense_ReturnsTrue_ForValidLicense()
    {
        // Arrange
        LoadSecretKeys();
        var hardwareId = HardwareUtils.GetHardwareId(); 
        var license = GenerateNodeLockedLicense(hardwareId);
        var licenseData = LicenseManager.SaveLicense(license);

        // Act
        var isValid = LicenseValidator.ValidateNodeLockedLicense(licenseData, hardwareId);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateNodeLockedLicense_ReturnsFalse_ForMismatchedHardwareId()
    {
        // Arrange
        LoadSecretKeys();
        const string hardwareId = "TestHardwareId";
        var license = GenerateNodeLockedLicense(hardwareId);
        var licenseData = LicenseManager.SaveLicense(license);

        // Act
        var isValid = LicenseValidator.ValidateNodeLockedLicense(licenseData, "IncorrectHardwareId");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateNodeLockedLicense_ReturnsFalse_ForExpiredLicense()
    {
        // Arrange
        LoadSecretKeys();
        const string hardwareId = "TestHardwareId";
        var license = GenerateNodeLockedLicense(hardwareId);
        license.ExpirationDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1));
        var licenseData = LicenseManager.SaveLicense(license);

        // Act
        var isValid = LicenseValidator.ValidateNodeLockedLicense(licenseData, hardwareId);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateSubscriptionLicense_ReturnsTrue_ForValidLicense()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateSubscriptionLicense();
        var licenseData = LicenseManager.SaveLicense(license);

        // Act
        var isValid = LicenseValidator.ValidateSubscriptionLicense(licenseData);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateSubscriptionLicense_ReturnsFalse_ForExpiredLicense()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateSubscriptionLicense();
        license.ExpirationDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1));
        var licenseData = LicenseManager.SaveLicense(license);

        // Act
        var isValid = LicenseValidator.ValidateSubscriptionLicense(licenseData);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateFloatingLicense_ReturnsTrue_ForValidLicense()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateFloatingLicense();
        var licenseData = LicenseManager.SaveLicense(license);

        // Act
        var isValid = LicenseValidator.ValidateFloatingLicense(licenseData, license.UserName, license.MaxActiveUsersCount);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateFloatingLicense_ReturnsFalse_ForIncorrectUserName()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateFloatingLicense();
        var licenseData = LicenseManager.SaveLicense(license);

        // Act
        var isValid = LicenseValidator.ValidateFloatingLicense(licenseData, "IncorrectUser", license.MaxActiveUsersCount);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateFloatingLicense_ReturnsFalse_ForIncorrectMaxActiveUsersCount()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateFloatingLicense();
        var licenseData = LicenseManager.SaveLicense(license);

        // Act
        var isValid = LicenseValidator.ValidateFloatingLicense(licenseData, license.UserName, 15); // Incorrect count

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void VerifyLicenseData_ReturnsFalse_ForInvalidSignature()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateStandardLicense();
        var licenseData = LicenseManager.SaveLicense(license);

        // Tamper with the signature 
        var (hash, signature, encryptedData, aesKey) = LicenseValidator.SplitLicenseData(licenseData);
        signature[0]++; // Modify a byte in the signature
        var combineLicenseDataMethod = typeof(LicenseManager).GetMethod("CombineLicenseData", BindingFlags.NonPublic | BindingFlags.Static);
        licenseData = (byte[])combineLicenseDataMethod?.Invoke(null, [hash, signature, encryptedData, aesKey])!;

        // Act
        var isValid = LicenseValidator.VerifyLicenseData(licenseData, out _);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void VerifyLicenseData_ReturnsFalse_ForTamperedData()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateStandardLicense();
        var licenseData = LicenseManager.SaveLicense(license);

        // Tamper with the encrypted data
        var (hash, signature, encryptedData, aesKey) = LicenseValidator.SplitLicenseData(licenseData);
        encryptedData[0]++; // Modify a byte in the encrypted data
        var combineLicenseDataMethod = typeof(LicenseManager).GetMethod("CombineLicenseData", BindingFlags.NonPublic | BindingFlags.Static);
        licenseData = (byte[])combineLicenseDataMethod?.Invoke(null, [hash, signature, encryptedData, aesKey])!;

        // Act
        var isValid = LicenseValidator.VerifyLicenseData(licenseData, out _);

        // Assert
        isValid.Should().BeFalse();
    }

    #region Private

    private void LoadSecretKeys()
    {
        var secretPath = Path.GetTempFileName();
        LicenseUtils.GenerateLicensingSecrets("MySecretTestKey", secretPath, "12345678-90ab-cdef-ghij-klmnopqrst");
        LicenseUtils.LoadLicensingSecrets("MySecretTestKey", secretPath);
    }

    // Helper methods to generate various licenses
    private StandardLicense GenerateStandardLicense()
    {
        return (StandardLicense)new StandardLicense("TestUser")
            .WithLicenseKey("SD2D-35G9-1502-X3DG-16VI-ELN2")
            .WithIssuer("Aegis Software")
            .WithExpiryDate(DateTime.UtcNow.AddDays(30));
    }

    private TrialLicense GenerateTrialLicense(TimeSpan trialPeriod)
    {
        return new TrialLicense(trialPeriod);
    }

    private NodeLockedLicense GenerateNodeLockedLicense(string hardwareId)
    {
        return new NodeLockedLicense(hardwareId);
    }

    private SubscriptionLicense GenerateSubscriptionLicense()
    {
        return new SubscriptionLicense("TestUser", TimeSpan.FromDays(365));
    }

    private FloatingLicense GenerateFloatingLicense()
    {
        return new FloatingLicense("TestUser", 10);
    }

    #endregion

}