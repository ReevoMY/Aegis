using Reevo.License.Domain;
using Reevo.License.Domain.Shared.Enum;
using Reevo.License.Domain.Exceptions;
using Reevo.License.Domain.Models;
using Sample.License.Web.Entities;
using Reevo.License.EntityFrameworkCore.Data;
using Reevo.License.EntityFrameworkCore.DTOs;
using Reevo.License.EntityFrameworkCore.Entities;
using Reevo.License.EntityFrameworkCore.Enums;
using Reevo.License.EntityFrameworkCore.Exceptions;
using Reevo.License.EntityFrameworkCore.Services;
using Reevo.License.Domain.Utilities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Sample.License.Web.Tests.Services;

public class LicenseServiceTests
{
    #region Fields

    private readonly LicenseDbContext _dbContext;
    private readonly LicenseService _licenseService;

    #endregion

    public LicenseServiceTests()
    {
        var options = new DbContextOptionsBuilder<LicenseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new LicenseDbContext(options);
        _licenseService = new LicenseService(_dbContext);

        // Initialize test data
        SeedDatabase();
        LoadSecretKeys();
    }

    #region GenerateLicenseAsync

    [Fact]
    public async Task GenerateLicenseAsync_ValidRequest_CreatesLicenseAndReturnsFile()
    {
        // Arrange
        var productId = _dbContext.Products.First().ProductId;
        var featureId = _dbContext.Features.First().FeatureId;
        var request = new LicenseGenerationRequest
        {
            LicenseType = LicenseType.Standard,
            ExpirationDate = DateTime.UtcNow.AddDays(30),
            ProductId = productId,
            IssuedTo = "Test User",
            FeatureIds = [featureId]
        };

        // Act
        var licenseFile = await _licenseService.GenerateLicenseAsync(request);

        // Assert 
        licenseFile.Should().NotBeNullOrEmpty();

        var license = await LicenseManager.LoadLicenseAsync(licenseFile);
        license.Should().NotBeNull();
        license.Should().BeOfType<StandardLicense>();
        license!.Type.Should().Be(request.LicenseType);
        license.ExpirationDate.Should().Be(request.ExpirationDate!.Value);
    }

    [Fact]
    public async Task GenerateLicenseAsync_InvalidProductId_ThrowsNotFoundException()
    {
        // Arrange
        var request = new LicenseGenerationRequest
        {
            LicenseType = LicenseType.Standard,
            ExpirationDate = DateTime.UtcNow.AddDays(30),
            ProductId = Guid.NewGuid(), // Invalid ProductId
            IssuedTo = "Test User"
        };

        // Act
        var act = async () => await _licenseService.GenerateLicenseAsync(request);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GenerateLicenseAsync_InvalidFeatureIds_ThrowsNotFoundException()
    {
        // Arrange
        var productId = _dbContext.Products.First().ProductId;
        var request = new LicenseGenerationRequest
        {
            LicenseType = LicenseType.Standard,
            ExpirationDate = DateTime.UtcNow.AddDays(30),
            ProductId = productId,
            IssuedTo = "Test User",
            FeatureIds = [Guid.NewGuid()] // Invalid FeatureId
        };

        // Act
        var act = async () => await _licenseService.GenerateLicenseAsync(request);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GenerateLicenseAsync_ExpirationDateInThePast_ThrowsBadRequestException()
    {
        // Arrange
        var productId = _dbContext.Products.First().ProductId;
        var request = new LicenseGenerationRequest
        {
            LicenseType = LicenseType.Standard,
            ExpirationDate = DateTime.UtcNow.AddDays(-30), // Past Expiration Date
            ProductId = productId,
            IssuedTo = "Test User"
        };

        // Act
        var act = async () => await _licenseService.GenerateLicenseAsync(request);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
    }

    #endregion

    #region ValidateLicenseAsync

    [Fact]
    public async Task ValidateLicenseAsync_ValidLicense_ReturnsValidResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Standard);
        var licenseFile = GenerateLicenseFile(license);

        // Act
        var result = await _licenseService.ValidateLicenseAsync(license.LicenseKey, licenseFile);

        // Assert
        result.IsValid.Should().BeTrue();
        result.License.Should().NotBeNull();
        result.Exception.Should().BeNull();
    }

    [Fact]
    public async Task ValidateLicenseAsync_MissingLicenseKey_ReturnsInvalidResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Standard);
        var licenseFile = GenerateLicenseFile(license);

        // Act
        var result = await _licenseService.ValidateLicenseAsync(string.Empty, licenseFile);

        // Assert
        result.IsValid.Should().BeFalse();
        result.License.Should().BeNull();
        result.Exception.Should().BeOfType<NotFoundException>();
    }

    [Fact]
    public async Task ValidateLicenseAsync_ExpiredLicense_ReturnsInvalidResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Standard, DateTime.UtcNow.AddDays(-30));
        var licenseFile = GenerateLicenseFile(license);

        // Act
        var result = await _licenseService.ValidateLicenseAsync(license.LicenseKey, licenseFile);

        // Assert
        result.IsValid.Should().BeFalse();
        result.License.Should().BeNull();
        result.Exception.Should().BeOfType<ExpiredLicenseException>();
    }

    [Fact]
    public async Task ValidateLicenseAsync_RevokedLicense_ReturnsInvalidResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Standard);
        license.Status = LicenseStatus.Revoked;
        _dbContext.Licenses.Update(license);
        await _dbContext.SaveChangesAsync();
        var licenseFile = GenerateLicenseFile(license);

        // Act
        var result = await _licenseService.ValidateLicenseAsync(license.LicenseKey, licenseFile);

        // Assert
        result.IsValid.Should().BeFalse();
        result.License.Should().BeNull();
        result.Exception.Should().BeOfType<LicenseValidationException>();
    }

    [Fact]
    public async Task ValidateLicenseAsync_TamperedLicense_ThrowsException()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Standard);
        var licenseFile = GenerateLicenseFile(license);
        licenseFile[5] = (byte)'X';

        // Act
        var result = await _licenseService.ValidateLicenseAsync(license.LicenseKey, licenseFile);

        // Assert
        result.IsValid.Should().BeFalse();
        result.License.Should().BeNull();
        result.Exception.Should().BeOfType<InvalidLicenseFormatException>();
    }

    [Fact]
    public async Task ValidateLicenseAsync_NodeLockedLicense_HardwareMismatch_ThrowException()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.NodeLocked, hardwareId: "12345678");
        var licenseFile = GenerateLicenseFile(license);
        var validationParams = new Dictionary<string, string?> { { "HardwareId", "87654321" } };


        // Act
        var result = await _licenseService.ValidateLicenseAsync(license.LicenseKey, licenseFile, validationParams);

        // Assert
        result.IsValid.Should().BeFalse();
        result.License.Should().BeNull();
        result.Exception.Should().BeOfType<LicenseValidationException>();
    }

    #endregion

    #region ActivateLicenseAsync

    [Fact]
    public async Task ActivateLicenseAsync_StandardLicense_ReturnsSuccessfulResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Standard);

        // Act
        var result = await _licenseService.ActivateLicenseAsync(license.LicenseKey);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Exception.Should().BeNull();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.Status.Should().Be(LicenseStatus.Active);
    }

    [Fact]
    public async Task ActivateLicenseAsync_TrialLicense_ReturnsSuccessfulResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Trial);

        // Act
        var result = await _licenseService.ActivateLicenseAsync(license.LicenseKey);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Exception.Should().BeNull();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.Status.Should().Be(LicenseStatus.Active);
    }

    [Fact]
    public async Task ActivateLicenseAsync_NodeLockedLicense_ReturnsSuccessfulResult()
    {
        // Arrange
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.NodeLocked);

        // Act
        var result = await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Exception.Should().BeNull();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.Status.Should().Be(LicenseStatus.Active);
        updatedLicense.HardwareId.Should().Be(hardwareId);
    }

    [Fact]
    public async Task ActivateLicenseAsync_ConcurrentLicense_BelowLimit_ReturnsSuccessfulResult()
    {
        // Arrange
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.Concurrent, maxActivations: 5);

        // Act
        var result = await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Exception.Should().BeNull();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.Status.Should().Be(LicenseStatus.Active);
        updatedLicense.ActiveUsersCount.Should().Be(1);

        var activation = await _dbContext.Activations.FirstOrDefaultAsync(a => a.LicenseId == license.Id);
        activation.Should().NotBeNull();
        activation!.MachineId.Should().Be(hardwareId);
    }

    [Fact]
    public async Task ActivateLicenseAsync_ConcurrentLicense_AtLimit_ReturnsUnsuccessfulResult()
    {
        // Arrange
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.Concurrent, maxActivations: 1);
        await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId);

        // Act
        var result = await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Exception.Should().BeOfType<MaximumActivationsReachedException>();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.ActiveUsersCount.Should().Be(1);
    }

    [Fact]
    public async Task ActivateLicenseAsync_FloatingLicense_BelowLimit_ReturnsSuccessfulResult()
    {
        // Arrange
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.Floating, maxActivations: 5);

        // Act
        var result = await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Exception.Should().BeNull();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.Status.Should().Be(LicenseStatus.Active);
        updatedLicense.ActiveUsersCount.Should().Be(1);

        var activation = await _dbContext.Activations.FirstOrDefaultAsync(a => a.LicenseId == license.Id);
        activation.Should().NotBeNull();
        activation!.MachineId.Should().Be(hardwareId);
    }

    [Fact]
    public async Task ActivateLicenseAsync_FloatingLicense_AtLimit_ReturnsUnsuccessfulResult()
    {
        // Arrange
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.Floating, maxActivations: 1);
        await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId);

        // Act
        var result = await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Exception.Should().BeOfType<MaximumActivationsReachedException>();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.ActiveUsersCount.Should().Be(1);
    }

    [Fact]
    public async Task ActivateLicenseAsync_SubscriptionLicense_ValidDate_ReturnsSuccessfulResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Subscription, DateTime.UtcNow.AddDays(30));

        // Act
        var result = await _licenseService.ActivateLicenseAsync(license.LicenseKey);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Exception.Should().BeNull();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.Status.Should().Be(LicenseStatus.Active);
    }

    [Fact]
    public async Task ActivateLicenseAsync_SubscriptionLicense_Expired_ReturnsUnsuccessfulResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Subscription, DateTime.UtcNow.AddDays(-30));

        // Act
        var result = await _licenseService.ActivateLicenseAsync(license.LicenseKey);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Exception.Should().BeOfType<ExpiredLicenseException>();
    }

    [Fact]
    public async Task ActivateLicenseAsync_InvalidLicenseKey_ReturnsUnsuccessfulResult()
    {
        // Act
        var result = await _licenseService.ActivateLicenseAsync("invalidkey");

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Exception.Should().BeOfType<NotFoundException>();
    }

    #endregion

    #region RevokeLicenseAsync

    [Fact]
    public async Task RevokeLicenseAsync_StandardLicense_ReturnsSuccessfulResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Standard);

        // Act
        var result = await _licenseService.RevokeLicenseAsync(license.LicenseKey);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Exception.Should().BeNull();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.Status.Should().Be(LicenseStatus.Revoked);
    }

    [Fact]
    public async Task RevokeLicenseAsync_TrialLicense_ReturnsSuccessfulResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Trial);

        // Act
        var result = await _licenseService.RevokeLicenseAsync(license.LicenseKey);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Exception.Should().BeNull();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.Status.Should().Be(LicenseStatus.Revoked);
    }

    [Fact]
    public async Task RevokeLicenseAsync_NodeLockedLicense_ReturnsSuccessfulResult()
    {
        // Arrange
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.NodeLocked, hardwareId: hardwareId);

        // Act
        var result = await _licenseService.RevokeLicenseAsync(license.LicenseKey, hardwareId);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Exception.Should().BeNull();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.Status.Should().Be(LicenseStatus.Revoked);
        updatedLicense.HardwareId.Should().BeNull();
    }

    [Fact]
    public async Task RevokeLicenseAsync_ConcurrentLicense_ReturnsSuccessfulResult()
    {
        // Arrange
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.Concurrent, maxActivations: 5);
        await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId); // Activate the license

        // Act
        var result = await _licenseService.RevokeLicenseAsync(license.LicenseKey, hardwareId);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Exception.Should().BeNull();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.ActiveUsersCount.Should().Be(0);

        var activation = await _dbContext.Activations.FirstOrDefaultAsync(a => a.LicenseId == license.Id);
        activation.Should().BeNull();
    }

    [Fact]
    public async Task RevokeLicenseAsync_FloatingLicense_ReturnsSuccessfulResult()
    {
        // Arrange
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.Floating, maxActivations: 5);
        await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId);

        // Act
        var result = await _licenseService.RevokeLicenseAsync(license.LicenseKey, hardwareId);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Exception.Should().BeNull();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.ActiveUsersCount.Should().Be(0);

        var activation = await _dbContext.Activations.FirstOrDefaultAsync(a => a.LicenseId == license.Id);
        activation.Should().BeNull();
    }

    [Fact]
    public async Task RevokeLicenseAsync_SubscriptionLicense_ReturnsSuccessfulResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Subscription, DateTime.UtcNow.AddDays(30));

        // Act
        var result = await _licenseService.RevokeLicenseAsync(license.LicenseKey);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Exception.Should().BeNull();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.Status.Should().Be(LicenseStatus.Revoked);
    }

    [Fact]
    public async Task RevokeLicenseAsync_InvalidLicenseKey_ReturnsUnsuccessfulResult()
    {
        // Act
        var result = await _licenseService.RevokeLicenseAsync("invalidkey");

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Exception.Should().BeOfType<NotFoundException>();
    }

    #endregion

    #region DisconnectConcurrentLicenseUser

    // ... (Implementation in progress)

    [Fact]
    public async Task DisconnectConcurrentLicenseUser_ValidRequest_ReturnsSuccessfulResult()
    {
        // Arrange
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.Concurrent, maxActivations: 5);
        await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId); // Activate the license

        // Act
        var result = await _licenseService.DisconnectConcurrentLicenseUser(license.LicenseKey, hardwareId);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Exception.Should().BeNull();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.ActiveUsersCount.Should().Be(0);

        var activation = await _dbContext.Activations.FirstOrDefaultAsync(a => a.LicenseId == license.Id);
        activation.Should().BeNull();
    }

    [Fact]
    public async Task DisconnectConcurrentLicenseUser_InvalidLicenseType_ReturnsUnsuccessfulResult()
    {
        // Arrange
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.NodeLocked, hardwareId: hardwareId);

        // Act
        var result = await _licenseService.DisconnectConcurrentLicenseUser(license.LicenseKey, hardwareId);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Exception.Should().BeOfType<InvalidLicenseFormatException>();
    }

    #endregion

    #region RenewLicenseAsync

    [Fact]
    public async Task RenewLicenseAsync_SubscriptionLicense_ValidDate_ReturnsSuccessfulResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Subscription, DateTime.UtcNow.AddDays(30));
        var newExpirationDate = DateTime.UtcNow.AddDays(60);

        // Act
        var result = await _licenseService.RenewLicenseAsync(license.LicenseKey, newExpirationDate);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Message.Should().Be("License renewed successfully.");
        result.LicenseFile.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RenewLicenseAsync_NonSubscriptionLicense_ReturnsUnsuccessfulResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Standard);
        var newExpirationDate = DateTime.UtcNow.AddDays(60);

        // Act
        var result = await _licenseService.RenewLicenseAsync(license.LicenseKey, newExpirationDate);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Message.Should().Be("Invalid license type. Only subscription licenses can be renewed.");
    }

    [Fact]
    public async Task RenewLicenseAsync_RevokedLicense_ReturnsUnsuccessfulResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Subscription, DateTime.UtcNow.AddDays(30));
        license.Status = LicenseStatus.Revoked;
        _dbContext.Licenses.Update(license);
        await _dbContext.SaveChangesAsync();
        var newExpirationDate = DateTime.UtcNow.AddDays(60);

        // Act
        var result = await _licenseService.RenewLicenseAsync(license.LicenseKey, newExpirationDate);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Message.Should().Be("License revoked.");
    }

    [Fact]
    public async Task RenewLicenseAsync_InvalidExpirationDate_ReturnsUnsuccessfulResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Subscription, DateTime.UtcNow.AddDays(30));
        var newExpirationDate = DateTime.UtcNow.AddDays(-30);

        // Act
        var result = await _licenseService.RenewLicenseAsync(license.LicenseKey, newExpirationDate);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Message.Should().Be("New expiration date cannot be in the past or before the current expiration date.");
    }

    #endregion

    #region HeartbeatAsync

    [Fact]
    public async Task HeartbeatAsync_ValidRequest_UpdatesLastHeartbeat()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Concurrent, maxActivations: 5);
        const string hardwareId = "12345678";
        await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId);
        var activation = await _dbContext.Activations.FirstAsync(a => a.LicenseId == license.Id);
        var initialHeartbeat = activation.LastHeartbeat;
        await Task.Delay(100);

        // Act
        var result = await _licenseService.HeartbeatAsync(license.LicenseKey, hardwareId);

        // Assert
        result.Should().BeTrue();

        var updatedActivation = await _dbContext.Activations.FirstAsync(a => a.LicenseId == license.Id);
        updatedActivation.LastHeartbeat.Should().BeAfter(initialHeartbeat);
    }

    [Fact]
    public async Task HeartbeatAsync_NonExistentActivation_ReturnsFalse()
    {
        // Arrange
        const string licenseKey = "NonExistentKey";
        const string hardwareId = "12345678";

        // Act
        var result = await _licenseService.HeartbeatAsync(licenseKey, hardwareId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Private 

    private void SeedDatabase()
    {
        _dbContext.Products.Add(new Product { ProductId = Guid.NewGuid(), ProductName = "Test Product" });
        _dbContext.Features.Add(new Feature { FeatureId = Guid.NewGuid(), FeatureName = "Feature 1" });
        _dbContext.Features.Add(new Feature { FeatureId = Guid.NewGuid(), FeatureName = "Feature 2" });
        _dbContext.SaveChanges();
        _dbContext.LicenseFeatures.Add(new LicenseFeature
        { ProductId = _dbContext.Products.First().ProductId, FeatureId = _dbContext.Features.First().FeatureId });
        _dbContext.LicenseFeatures.Add(new LicenseFeature
        { ProductId = _dbContext.Products.First().ProductId, FeatureId = _dbContext.Features.Last().FeatureId });
        _dbContext.SaveChanges();
    }

    private void LoadSecretKeys()
    {
        var secretPath = Path.GetTempFileName();
        LicenseUtils.GenerateLicensingSecrets("MySecretTestKey", secretPath, "12345678-90ab-cdef-ghij-klmnopqrst");
        LicenseUtils.LoadLicensingSecrets("MySecretTestKey", secretPath);
    }


    private Reevo.License.EntityFrameworkCore.Entities.License CreateAndSaveLicense(LicenseType licenseType, DateTime? expirationDate = null,
        string? hardwareId = null, int? maxActivations = null)
    {
        var productId = _dbContext.Products.First().ProductId;
        var licenseFeature = _dbContext.LicenseFeatures.First();
        var license = new MyLicense
        {
            Type = licenseType,
            ProductId = productId,
            IssuedTo = "Test User",
            HardwareId = hardwareId,
            MaxActiveUsersCount = maxActivations,
            IssuedOn = DateTime.UtcNow,
            ExpirationDate = expirationDate,
            SubscriptionExpiryDate = licenseType == LicenseType.Subscription ? expirationDate : null,
            LicenseFeatures = [licenseFeature]
        };

        _dbContext.Licenses.Add(license);
        _dbContext.SaveChanges();

        return license;
    }

    private byte[] GenerateLicenseFile(Reevo.License.EntityFrameworkCore.Entities.License license)
    {
        var baseLicense = new BaseLicense
        {
            LicenseId = license.Id,
            LicenseKey = license.LicenseKey,
            Type = license.Type,
            IssuedOn = license.IssuedOn,
            ExpirationDate = license.ExpirationDate,
            Features = license.LicenseFeatures.ToDictionary(lf => lf.Feature.FeatureName, lf => lf.IsEnabled),
            Issuer = license.Issuer
        };
        return license.Type switch
        {
            LicenseType.Standard => LicenseManager.SaveLicense(new StandardLicense(baseLicense, license.IssuedTo)),
            LicenseType.Trial => LicenseManager.SaveLicense(new TrialLicense(baseLicense,
                license.ExpirationDate!.Value - DateTime.UtcNow)),
            LicenseType.NodeLocked => LicenseManager.SaveLicense(
                new NodeLockedLicense(baseLicense, license.HardwareId!)),
            LicenseType.Subscription => LicenseManager.SaveLicense(new SubscriptionLicense(baseLicense,
                license.IssuedTo,
                license.ExpirationDate!.Value - DateTime.UtcNow)),
            LicenseType.Floating => LicenseManager.SaveLicense(new FloatingLicense(baseLicense, license.IssuedTo,
                license.MaxActiveUsersCount!.Value)),
            LicenseType.Concurrent => LicenseManager.SaveLicense(new ConcurrentLicense(baseLicense, license.IssuedTo,
                license.MaxActiveUsersCount!.Value)),
            _ => throw new InvalidLicenseFormatException("Invalid license type.")
        };
    }

    #endregion

}