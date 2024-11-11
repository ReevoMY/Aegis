using Aegis.Enums;
using Aegis.Models;
using FluentAssertions;

namespace Aegis.Tests;

public class LicenseGeneratorTests
{
    [Fact]
    public void GenerateStandardLicense_CreatesValidLicense()
    {
        // Arrange
        const string userName = "TestUser";

        // Act
        var license = LicenseGenerator.GenerateStandardLicense(userName);

        // Assert
        license.Should().NotBeNull();
        license.Should().BeOfType<StandardLicense>();
        license.LicenseId.Should().NotBeEmpty();
        license.LicenseKey.Should().NotBeNullOrEmpty();
        license.Type.Should().Be(LicenseType.Standard);
        license.IssuedOn.Date.Should().Be(DateTime.UtcNow.Date);
        license.UserName.Should().Be(userName);
    }

    [Fact]
    public void GenerateTrialLicense_CreatesValidLicense()
    {
        // Arrange
        var trialPeriod = TimeSpan.FromDays(14);

        // Act
        var license = LicenseGenerator.GenerateTrialLicense(trialPeriod);

        // Assert
        license.Should().NotBeNull();
        license.Should().BeOfType<TrialLicense>();
        license.TrialPeriod.Should().Be(trialPeriod);
        license.LicenseId.Should().NotBeEmpty();
        license.LicenseKey.Should().NotBeNullOrEmpty();
        license.Type.Should().Be(LicenseType.Trial);
        license.IssuedOn.Date.Should().Be(DateTime.UtcNow.Date);
        license.ExpirationDate!.Value.Date.Should().Be(DateTime.UtcNow.Add(trialPeriod).Date);
    }

    [Fact]
    public void GenerateNodeLockedLicense_CreatesValidLicense_WithGeneratedHardwareId()
    {
        // Arrange & Act
        var license = LicenseGenerator.GenerateNodeLockedLicense(); // No hardwareId provided

        // Assert
        license.Should().NotBeNull();
        license.Should().BeOfType<NodeLockedLicense>();
        license.Type.Should().Be(LicenseType.NodeLocked);
        license.LicenseId.Should().NotBeEmpty();
        license.LicenseKey.Should().NotBeNullOrEmpty();
        license.IssuedOn.Date.Should().Be(DateTime.UtcNow.Date);
        license.HardwareId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateNodeLockedLicense_CreatesValidLicense_WithProvidedHardwareId()
    {
        // Arrange
        const string hardwareId = "test-hardware-id";

        // Act
        var license = LicenseGenerator.GenerateNodeLockedLicense(hardwareId);

        // Assert
        license.Should().NotBeNull();
        license.Should().BeOfType<NodeLockedLicense>();
        license.Type.Should().Be(LicenseType.NodeLocked);
        license.LicenseId.Should().NotBeEmpty();
        license.LicenseKey.Should().NotBeNullOrEmpty();
        license.IssuedOn.Date.Should().Be(DateTime.UtcNow.Date);
        license.HardwareId.Should().Be(hardwareId);
    }

    [Fact]
    public void GenerateSubscriptionLicense_CreatesValidLicense()
    {
        // Arrange
        const string userName = "TestUser";
        var subscriptionDuration = TimeSpan.FromDays(365);

        // Act
        var license = LicenseGenerator.GenerateSubscriptionLicense(userName, subscriptionDuration);

        // Assert
        license.Should().NotBeNull();
        license.Should().BeOfType<SubscriptionLicense>();
        license.UserName.Should().Be(userName);
        license.SubscriptionDuration.Should().Be(subscriptionDuration);
        license.Type.Should().Be(LicenseType.Subscription);
        license.IssuedOn.Date.Should().Be(DateTime.UtcNow.Date);
        license.SubscriptionStartDate.Date.Should().Be(DateTime.UtcNow.Date);
    }

    [Fact]
    public void GenerateFloatingLicense_CreatesValidLicense()
    {
        // Arrange
        const string userName = "TestUser";
        const int maxActiveUsersCount = 10;

        // Act
        var license = LicenseGenerator.GenerateFloatingLicense(userName, maxActiveUsersCount);

        // Assert
        license.Should().NotBeNull();
        license.Should().BeOfType<FloatingLicense>();
        license.UserName.Should().Be(userName);
        license.MaxActiveUsersCount.Should().Be(maxActiveUsersCount);
        license.Type.Should().Be(LicenseType.Floating);
        license.IssuedOn.Date.Should().Be(DateTime.UtcNow.Date);
    }

    [Fact]
    public void GenerateConcurrentLicense_CreatesValidLicense()
    {
        // Arrange
        const string userName = "TestUser";
        const int maxActiveUsersCount = 4;

        // Act
        var license = LicenseGenerator.GenerateConcurrentLicense(userName, maxActiveUsersCount);

        // Assert
        license.Should().NotBeNull();
        license.Should().BeOfType<ConcurrentLicense>();
        license.UserName.Should().Be(userName);
        license.MaxActiveUsersCount.Should().Be(maxActiveUsersCount);
        license.Type.Should().Be(LicenseType.Concurrent);
        license.IssuedOn.Date.Should().Be(DateTime.UtcNow.Date);
    }
}