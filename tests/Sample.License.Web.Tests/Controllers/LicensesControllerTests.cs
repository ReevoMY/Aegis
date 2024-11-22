﻿using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Reevo.License.Domain;
using Reevo.License.Domain.Shared.Enum;
using Reevo.License.Domain.Exceptions;
using Reevo.License.Domain.Models;
using Sample.License.Web.Attributes;
using Sample.License.Web.Controllers;
using Sample.License.Web.Data.Context;
using Sample.License.Web.DTOs;
using Sample.License.Web.Entities;
using Sample.License.Web.Middlewares;
using Sample.License.Web.Services;
using Reevo.License.EntityFrameworkCore.DTOs;
using Reevo.License.EntityFrameworkCore.Entities;
using Reevo.License.EntityFrameworkCore.Enums;
using Reevo.License.EntityFrameworkCore.Exceptions;
using Reevo.License.EntityFrameworkCore.Services;
using Reevo.License.Domain.Utilities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace Sample.License.Web.Tests.Controllers;

public class LicensesControllerTests
{
    #region Fields

    private readonly IAuthService _authService;
    private readonly LicensesController _controller;
    private readonly ApplicationDbContext _dbContext;
    private readonly HttpContext _httpContext;
    private readonly IOptions<JwtSettings> _jwtSettings;
    private readonly LicenseService _licenseService;

    #endregion

    // Constructor to set up the test environment
    public LicensesControllerTests()
    {
        // 1. Set up an in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);

        // 2. Configure JWT settings for testing
        _jwtSettings = Options.Create(new JwtSettings
        {
            Secret = "ThisIsMyVerySecretKeyForJWT!",
            Salt = "SaltForPasswordHashing",
            AccessTokenExpirationInDays = 1,
            RefreshTokenExpirationInDays = 7
        });

        // 3. Create instances of AuthService and AuthenticationController for testing
        _authService = new AuthService(_dbContext, _jwtSettings);
        _licenseService = new LicenseService(_dbContext);
        _controller = new LicensesController(_licenseService);

        // 4. Mock HttpContext and User for authorization testing
        _httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal()
        };

        // 5. Mock HttpContext and User for authorization testing
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _httpContext,
            ActionDescriptor = new ControllerActionDescriptor { FilterDescriptors = [] }
        };

        // Initialize test data
        SeedDatabase();
        LoadSecretKeys();
    }

    #region Generate

    [Fact]
    public async Task Generate_ValidRequestWithAdminRole_ReturnsOkResultWithLicense()
    {
        // Arrange
        SetupUser("Admin"); // Set up user with Admin role
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
        var result = await ExecuteControllerAction(() => _controller.Generate(request));

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var license = okResult.Value.Should().BeOfType<byte[]>().Subject;
        license.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Generate_ValidRequestWithUserRole_ReturnsUnauthorized()
    {
        // Arrange
        SetupUser();
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
        var result = await ExecuteControllerAction(() => _controller.Generate(request));

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Generate_InvalidProductId_ThrowsNotFoundException()
    {
        // Arrange
        SetupUser("Admin");
        var request = new LicenseGenerationRequest
        {
            LicenseType = LicenseType.Standard,
            ExpirationDate = DateTime.UtcNow.AddDays(30),
            ProductId = Guid.NewGuid(), // Invalid ProductId
            IssuedTo = "Test User"
        };

        // Act
        var act = async () => await ExecuteControllerAction(() => _controller.Generate(request));

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Generate_InvalidFeatureIds_ThrowsNotFoundException()
    {
        // Arrange
        SetupUser("Admin");
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
        var act = async () => await ExecuteControllerAction(() => _controller.Generate(request));

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Generate_ExpirationDateInThePast_ThrowsBadRequestException()
    {
        // Arrange
        SetupUser("Admin");
        var productId = _dbContext.Products.First().ProductId;
        var request = new LicenseGenerationRequest
        {
            LicenseType = LicenseType.Standard,
            ExpirationDate = DateTime.UtcNow.AddDays(-30), // Past Expiration Date
            ProductId = productId,
            IssuedTo = "Test User"
        };

        // Act
        var act = async () => await ExecuteControllerAction(() => _controller.Generate(request));

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
    }

    #endregion

    #region Validate Tests

    [Fact]
    public async Task Validate_ValidLicenseWithAdminRole_ReturnsOkResult()
    {
        // Arrange
        SetupUser("Admin");
        var license = CreateAndSaveLicense(LicenseType.Standard);
        var licenseFile = GenerateLicenseFile(license);
        var validationParams = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            { "UserName", license.IssuedTo },
            { "SerialNumber", license.LicenseKey }
        });

        // Create a mock IFormFile
        var fileMock = new Mock<IFormFile>();
        const string fileName = "license.lic";
        var ms = new MemoryStream(licenseFile);
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(ms.Length);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Validate(license.LicenseKey, validationParams, fileMock.Object));

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be("License is valid");
    }

    [Fact]
    public async Task Validate_ValidLicenseWithUserRole_ReturnsOkResult()
    {
        // Arrange
        SetupUser();
        var license = CreateAndSaveLicense(LicenseType.Standard);
        var licenseFile = GenerateLicenseFile(license);
        var validationParams = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            { "UserName", license.IssuedTo },
            { "SerialNumber", license.LicenseKey }
        });

        // Create a mock IFormFile
        var fileMock = new Mock<IFormFile>();
        const string fileName = "license.lic";
        var ms = new MemoryStream(licenseFile);
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(ms.Length);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Validate(license.LicenseKey, validationParams, fileMock.Object));

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be("License is valid");
    }

    [Fact]
    public async Task Validate_MissingLicenseKey_ReturnsBadRequest()
    {
        // Arrange
        SetupUser("Admin");

        // Act
        var result = await ExecuteControllerAction(() => _controller.Validate(string.Empty, "{}", null!));

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("License key is required.");
    }

    [Fact]
    public async Task Validate_MissingLicenseFile_ReturnsBadRequest()
    {
        // Arrange
        SetupUser("Admin");

        // Act
        var result = await ExecuteControllerAction(() => _controller.Validate("some-license-key", "{}", null!));

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("License file is required.");
    }

    [Fact]
    public async Task Validate_InvalidLicenseFormat_ReturnsBadRequest()
    {
        // Arrange
        SetupUser("Admin");
        var licenseFile = "Invalid License Data"u8.ToArray();

        // Create a mock IFormFile
        var fileMock = new Mock<IFormFile>();
        const string fileName = "license.lic";
        var ms = new MemoryStream(licenseFile);
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(ms.Length);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Validate("some-license-key", "{}", fileMock.Object));

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeOfType<NotFoundException>();
    }

    [Fact]
    public async Task Validate_TamperedLicense_ReturnsBadRequest()
    {
        // Arrange
        SetupUser("Admin");
        var license = CreateAndSaveLicense(LicenseType.Standard);
        var licenseFile = GenerateLicenseFile(license);
        licenseFile[5] = (byte)'X'; // Tamper with the license file

        // Create a mock IFormFile
        var fileMock = new Mock<IFormFile>();
        var ms = new MemoryStream(licenseFile);
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns("license.lic");
        fileMock.Setup(f => f.Length).Returns(ms.Length);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Validate(license.LicenseKey, "{}", fileMock.Object));

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeOfType<InvalidLicenseFormatException>();
    }

    [Fact]
    public async Task Validate_ExpiredLicense_ReturnsBadRequest()
    {
        // Arrange
        SetupUser("Admin");
        var license = CreateAndSaveLicense(LicenseType.Standard, DateTime.UtcNow.AddDays(-30));
        var licenseFile = GenerateLicenseFile(license);

        // Create a mock IFormFile
        var fileMock = new Mock<IFormFile>();
        var content = licenseFile;
        var fileName = "license.lic";
        var ms = new MemoryStream(content);
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(ms.Length);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Validate(license.LicenseKey, "{}", fileMock.Object));

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeOfType<ExpiredLicenseException>();
    }

    [Fact]
    public async Task Validate_RevokedLicense_ReturnsBadRequest()
    {
        // Arrange
        SetupUser("Admin");
        var license = CreateAndSaveLicense(LicenseType.Standard);
        license.Status = LicenseStatus.Revoked;
        _dbContext.Licenses.Update(license);
        await _dbContext.SaveChangesAsync();
        var licenseFile = GenerateLicenseFile(license);

        // Create a mock IFormFile
        var fileMock = new Mock<IFormFile>();
        var content = licenseFile;
        var fileName = "license.lic";
        var ms = new MemoryStream(content);
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(ms.Length);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Validate(license.LicenseKey, "{}", fileMock.Object));

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeOfType<LicenseValidationException>();
    }

    [Fact]
    public async Task Validate_NodeLockedLicense_HardwareMismatch_ReturnsBadRequest()
    {
        // Arrange
        SetupUser("Admin");
        var license = CreateAndSaveLicense(LicenseType.NodeLocked, hardwareId: "12345678");
        var licenseFile = GenerateLicenseFile(license);
        var validationParams =
            JsonSerializer.Serialize(new Dictionary<string, string?> { { "HardwareId", "87654321" } });

        // Create a mock IFormFile
        var fileMock = new Mock<IFormFile>();
        const string fileName = "license.lic";
        var ms = new MemoryStream(licenseFile);
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(ms.Length);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Validate(license.LicenseKey, validationParams, fileMock.Object));

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeOfType<LicenseValidationException>();
    }

    #endregion

    #region Activate Tests

    [Fact]
    public async Task Activate_StandardLicenseWithAdminRole_ReturnsOkResult()
    {
        // Arrange
        SetupUser("Admin");
        var license = CreateAndSaveLicense(LicenseType.Standard);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Activate(license.LicenseKey, null));

        // Assert
        result.Should().BeOfType<OkResult>();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.Status.Should().Be(LicenseStatus.Active);
    }

    [Fact]
    public async Task Activate_TrialLicenseWithAdminRole_ReturnsOkResult()
    {
        // Arrange
        SetupUser("Admin");
        var license = CreateAndSaveLicense(LicenseType.Trial);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Activate(license.LicenseKey, null));

        // Assert
        result.Should().BeOfType<OkResult>();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.Status.Should().Be(LicenseStatus.Active);
    }

    [Fact]
    public async Task Activate_NodeLockedLicenseWithAdminRole_ReturnsOkResult()
    {
        // Arrange
        SetupUser("Admin");
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.NodeLocked);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Activate(license.LicenseKey, hardwareId));

        // Assert
        result.Should().BeOfType<OkResult>();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.Status.Should().Be(LicenseStatus.Active);
        updatedLicense.HardwareId.Should().Be(hardwareId);;
    }

    [Fact]
    public async Task Activate_ConcurrentLicenseWithAdminRole_BelowLimit_ReturnsOkResult()
    {
        // Arrange
        SetupUser("Admin");
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.Concurrent, maxActivations: 5);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Activate(license.LicenseKey, hardwareId));

        // Assert
        result.Should().BeOfType<OkResult>();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.Status.Should().Be(LicenseStatus.Active);
        updatedLicense.ActiveUsersCount.Should().Be(1);

        var activation = await _dbContext.Activations.FirstOrDefaultAsync(a => a.LicenseId == license.Id);
        activation.Should().NotBeNull();
        activation!.MachineId.Should().Be(hardwareId);
    }

    [Fact]
    public async Task Activate_ConcurrentLicenseWithAdminRole_AtLimit_ReturnsBadRequest()
    {
        // Arrange
        SetupUser("Admin");
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.Concurrent, maxActivations: 1);
        await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Activate(license.LicenseKey, hardwareId));

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeOfType<MaximumActivationsReachedException>();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.ActiveUsersCount.Should().Be(1);
    }

    [Fact]
    public async Task Activate_FloatingLicenseWithAdminRole_BelowLimit_ReturnsOkResult()
    {
        // Arrange
        SetupUser("Admin");
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.Floating, maxActivations: 5);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Activate(license.LicenseKey, hardwareId));

        // Assert
        result.Should().BeOfType<OkResult>();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.Status.Should().Be(LicenseStatus.Active);
        updatedLicense.ActiveUsersCount.Should().Be(1);

        var activation = await _dbContext.Activations.FirstOrDefaultAsync(a => a.LicenseId == license.Id);
        activation.Should().NotBeNull();
        activation!.MachineId.Should().Be(hardwareId);
    }

    [Fact]
    public async Task Activate_FloatingLicenseWithAdminRole_AtLimit_ReturnsBadRequest()
    {
        // Arrange
        SetupUser("Admin");
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.Floating, maxActivations: 1);
        await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Activate(license.LicenseKey, hardwareId));

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeOfType<MaximumActivationsReachedException>();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.ActiveUsersCount.Should().Be(1);
    }

    [Fact]
    public async Task Activate_SubscriptionLicenseWithAdminRole_ValidDate_ReturnsOkResult()
    {
        // Arrange
        SetupUser("Admin");
        var license = CreateAndSaveLicense(LicenseType.Subscription, DateTime.UtcNow.AddDays(30));

        // Act
        var result = await ExecuteControllerAction(() => _controller.Activate(license.LicenseKey, null));

        // Assert
        result.Should().BeOfType<OkResult>();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.Status.Should().Be(LicenseStatus.Active);
    }

    [Fact]
    public async Task Activate_SubscriptionLicenseWithAdminRole_Expired_ReturnsBadRequest()
    {
        // Arrange
        SetupUser("Admin");
        var license = CreateAndSaveLicense(LicenseType.Subscription, DateTime.UtcNow.AddDays(-30));

        // Act
        var result = await ExecuteControllerAction(() => _controller.Activate(license.LicenseKey, null));

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeOfType<ExpiredLicenseException>();
    }

    [Fact]
    public async Task Activate_InvalidLicenseKey_ReturnsBadRequest()
    {
        // Arrange
        SetupUser("Admin");

        // Act
        var result = await ExecuteControllerAction(() => _controller.Activate("invalid", null));

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeOfType<NotFoundException>();
    }

    [Fact]
    public async Task Activate_WithUserRole_ReturnsUnauthorized()
    {
        // Arrange
        SetupUser();
        var license = CreateAndSaveLicense(LicenseType.Standard);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Activate(license.LicenseKey, null));

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region Revoke Tests

    [Fact]
    public async Task Revoke_StandardLicenseWithAdminRole_ReturnsOkResult()
    {
        // Arrange
        SetupUser("Admin");
        var license = CreateAndSaveLicense(LicenseType.Standard);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Revoke(license.LicenseKey, null));

        // Assert
        result.Should().BeOfType<OkResult>();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.Status.Should().Be(LicenseStatus.Revoked);
    }

    [Fact]
    public async Task Revoke_TrialLicenseWithAdminRole_ReturnsOkResult()
    {
        // Arrange
        SetupUser("Admin");
        var license = CreateAndSaveLicense(LicenseType.Trial);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Revoke(license.LicenseKey, null));

        // Assert
        result.Should().BeOfType<OkResult>();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.Status.Should().Be(LicenseStatus.Revoked);
    }

    [Fact]
    public async Task Revoke_NodeLockedLicenseWithAdminRole_ReturnsOkResult()
    {
        // Arrange
        SetupUser("Admin");
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.NodeLocked, hardwareId: hardwareId);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Revoke(license.LicenseKey, hardwareId));

        // Assert
        result.Should().BeOfType<OkResult>();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.Status.Should().Be(LicenseStatus.Revoked);
        updatedLicense.HardwareId.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task Revoke_ConcurrentLicenseWithAdminRole_ReturnsOkResult()
    {
        // Arrange
        SetupUser("Admin");
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.Concurrent, maxActivations: 5);
        await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Revoke(license.LicenseKey, hardwareId));

        // Assert
        result.Should().BeOfType<OkResult>();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.ActiveUsersCount.Should().Be(0);

        var activation = await _dbContext.Activations.FirstOrDefaultAsync(a => a.LicenseId == license.Id);
        activation.Should().BeNull();
    }

    [Fact]
    public async Task Revoke_FloatingLicenseWithAdminRole_ReturnsOkResult()
    {
        // Arrange
        SetupUser("Admin");
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.Floating, maxActivations: 5);
        await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Revoke(license.LicenseKey, hardwareId));

        // Assert
        result.Should().BeOfType<OkResult>();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.ActiveUsersCount.Should().Be(0);

        var activation = await _dbContext.Activations.FirstOrDefaultAsync(a => a.LicenseId == license.Id);
        activation.Should().BeNull();
    }

    [Fact]
    public async Task Revoke_SubscriptionLicenseWithAdminRole_ReturnsOkResult()
    {
        // Arrange
        SetupUser("Admin");
        var license = CreateAndSaveLicense(LicenseType.Subscription, DateTime.UtcNow.AddDays(30));

        // Act
        var result = await ExecuteControllerAction(() => _controller.Revoke(license.LicenseKey, null));

        // Assert
        result.Should().BeOfType<OkResult>();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.Status.Should().Be(LicenseStatus.Revoked);
    }

    [Fact]
    public async Task Revoke_InvalidLicenseKey_ReturnsNotFound()
    {
        // Arrange
        SetupUser("Admin");

        // Act
        var result = await ExecuteControllerAction(() => _controller.Revoke("invalid", null));

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Revoke_WithUserRole_ReturnsUnauthorized()
    {
        // Arrange
        SetupUser();
        var license = CreateAndSaveLicense(LicenseType.Standard);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Revoke(license.LicenseKey, null));

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region Disconnect Tests

    [Fact]
    public async Task Disconnect_ConcurrentLicenseWithUserRole_ReturnsOkResult()
    {
        // Arrange
        SetupUser();
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.Concurrent, maxActivations: 5);
        await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Disconnect(license.LicenseKey, hardwareId));

        // Assert
        result.Should().BeOfType<OkResult>();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.ActiveUsersCount.Should().Be(0);

        var activation = await _dbContext.Activations.FirstOrDefaultAsync(a => a.LicenseId == license.Id);
        activation.Should().BeNull();
    }

    [Fact]
    public async Task Disconnect_InvalidLicenseType_ReturnsNotFound()
    {
        // Arrange
        SetupUser();
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.NodeLocked, hardwareId: hardwareId);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Disconnect(license.LicenseKey, hardwareId));

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeOfType<InvalidLicenseFormatException>();
    }

    [Fact]
    public async Task Disconnect_WithAdminRole_ReturnsUnauthorized()
    {
        // Arrange
        SetupUser("Admin");
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.Concurrent, maxActivations: 5);

        // Act
        var result = await ExecuteControllerAction(() => _controller.Disconnect(license.LicenseKey, hardwareId));

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region Renew Tests

    [Fact]
    public async Task RenewLicense_SubscriptionLicenseWithAdminRole_ValidDate_ReturnsOkResult()
    {
        // Arrange
        SetupUser("Admin");
        var license = CreateAndSaveLicense(LicenseType.Subscription, DateTime.UtcNow.AddDays(30));
        var newExpirationDate = DateTime.UtcNow.AddDays(60);
        var request = new RenewLicenseRequest
        {
            LicenseKey = license.LicenseKey,
            NewExpirationDate = newExpirationDate
        };

        // Act
        var result = await ExecuteControllerAction(() => _controller.RenewLicense(request));

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var updatedLicenseFile = okResult.Value.Should().BeOfType<byte[]>().Subject;
        updatedLicenseFile.Should().NotBeNullOrEmpty();

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.Id);
        updatedLicense!.SubscriptionExpiryDate.Should().Be(newExpirationDate);
    }

    [Fact]
    public async Task RenewLicense_NonSubscriptionLicense_ReturnsBadRequest()
    {
        // Arrange
        SetupUser("Admin");
        var license = CreateAndSaveLicense(LicenseType.Standard);
        var newExpirationDate = DateTime.UtcNow.AddDays(60);
        var request = new RenewLicenseRequest
        {
            LicenseKey = license.LicenseKey,
            NewExpirationDate = newExpirationDate
        };

        // Act
        var result = await ExecuteControllerAction(() => _controller.RenewLicense(request));

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RenewLicense_RevokedLicense_ReturnsBadRequest()
    {
        // Arrange
        SetupUser("Admin");
        var license = CreateAndSaveLicense(LicenseType.Subscription, DateTime.UtcNow.AddDays(30));
        license.Status = LicenseStatus.Revoked;
        _dbContext.Licenses.Update(license);
        await _dbContext.SaveChangesAsync();
        var newExpirationDate = DateTime.UtcNow.AddDays(60);
        var request = new RenewLicenseRequest
        {
            LicenseKey = license.LicenseKey,
            NewExpirationDate = newExpirationDate
        };

        // Act
        var result = await ExecuteControllerAction(() => _controller.RenewLicense(request));

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RenewLicense_InvalidExpirationDate_ReturnsBadRequest()
    {
        // Arrange
        SetupUser("Admin");
        var license = CreateAndSaveLicense(LicenseType.Subscription, DateTime.UtcNow.AddDays(30));
        var newExpirationDate = DateTime.UtcNow.AddDays(-30); // Past date
        var request = new RenewLicenseRequest
        {
            LicenseKey = license.LicenseKey,
            NewExpirationDate = newExpirationDate
        };

        // Act
        var result = await ExecuteControllerAction(() => _controller.RenewLicense(request));

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RenewLicense_WithUserRole_ReturnsUnauthorized()
    {
        // Arrange
        SetupUser();
        var license = CreateAndSaveLicense(LicenseType.Subscription, DateTime.UtcNow.AddDays(30));
        var newExpirationDate = DateTime.UtcNow.AddDays(60);
        var request = new RenewLicenseRequest
        {
            LicenseKey = license.LicenseKey,
            NewExpirationDate = newExpirationDate
        };

        // Act
        var result = await ExecuteControllerAction(() => _controller.RenewLicense(request));

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region Heartbeat Tests

    [Fact]
    public async Task Heartbeat_ValidRequestWithUserRole_ReturnsOkResult()
    {
        // Arrange
        SetupUser();
        var license = CreateAndSaveLicense(LicenseType.Concurrent, maxActivations: 5);
        const string machineId = "12345678";
        await _licenseService.ActivateLicenseAsync(license.LicenseKey, machineId);
        var request = new HeartbeatRequest { LicenseKey = license.LicenseKey, MachineId = machineId };

        // Act
        var result = await ExecuteControllerAction(() => _controller.Heartbeat(request));

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Heartbeat_NonExistentActivation_ReturnsNotFound()
    {
        // Arrange
        SetupUser();
        var request = new HeartbeatRequest { LicenseKey = "non-existent-key", MachineId = "12345678" };

        // Act
        var result = await ExecuteControllerAction(() => _controller.Heartbeat(request));

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Heartbeat_WithAdminRole_ReturnsUnauthorized()
    {
        // Arrange
        SetupUser("Admin");
        var license = CreateAndSaveLicense(LicenseType.Concurrent, maxActivations: 5);
        const string machineId = "12345678";
        var request = new HeartbeatRequest { LicenseKey = license.LicenseKey, MachineId = machineId };

        // Act
        var result = await ExecuteControllerAction(() => _controller.Heartbeat(request));

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region Private

    private void SeedDatabase()
    {
        // Add product and features
        _dbContext.Products.Add(new Product { ProductId = Guid.NewGuid(), ProductName = "Test Product" });
        _dbContext.Features.Add(new Feature { FeatureId = Guid.NewGuid(), FeatureName = "Feature 1" });
        _dbContext.Features.Add(new Feature { FeatureId = Guid.NewGuid(), FeatureName = "Feature 2" });
        _dbContext.SaveChanges();

        // Link features to product
        _dbContext.LicenseFeatures.Add(new LicenseFeature
        { ProductId = _dbContext.Products.First().ProductId, FeatureId = _dbContext.Features.First().FeatureId });
        _dbContext.LicenseFeatures.Add(new LicenseFeature
        { ProductId = _dbContext.Products.First().ProductId, FeatureId = _dbContext.Features.Last().FeatureId });
        _dbContext.SaveChanges();
    }

    private void LoadSecretKeys()
    {
        var secretPath = Path.GetTempFileName();
        LicenseUtils.GenerateLicensingSecrets("MySecretTestKey", secretPath, "12345678-90ab-def-gif-testing");
        LicenseUtils.LoadLicensingSecrets("MySecretTestKey", secretPath);
    }

    private void SetupUser(string role = "User", string username = "tester", bool useApiKey = false)
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = username + "@example.com",
            Username = username,
            Role = role
        };
        if (useApiKey)
        {
            // Mock API key usage
            const string apiKey = "YourTestApiKey";
            user.ApiKey = apiKey;
            _httpContext.Request.Headers["X-API-KEY"] = apiKey;
        }
        else
        {
            // Generate a valid JWT token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = SHA256.HashData(Encoding.ASCII.GetBytes(_jwtSettings.Value.Secret));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new(ClaimTypes.NameIdentifier, userId.ToString()),
                    new(ClaimTypes.Name, username),
                    new(ClaimTypes.Role, role)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = tokenHandler.WriteToken(token);

            // Mock JWT token in the Authorization header
            _httpContext.Request.Headers.Authorization = $"Bearer {jwtToken}";
        }

        _dbContext.Roles.AddRange([new Role { Name = "User" }, new Role { Name = "Admin" }]);
        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();
    }

    private async Task<IActionResult> ExecuteControllerAction(Expression<Func<Task<IActionResult>>> expression)
    {
        // 1. Get roles and allowAnonymous from the AuthorizeMiddlewareAttribute
        // Get the MethodInfo of the target method
        var methodInfo = ((MethodCallExpression)expression.Body).Method;

        // Get the custom attribute
        var authorizeAttribute = methodInfo.GetCustomAttribute<AuthorizeMiddlewareAttribute>();
        var allowedRoles = authorizeAttribute?.Roles ?? ["User"];
        var allowAnonymous = authorizeAttribute?.AllowAnonymous ?? false;

        var action = expression.Compile();

        // 2. Perform Authorization Check
        var middleware = new AuthorizationMiddleware(async _ => await action(), _authService, _dbContext, allowedRoles,
            allowAnonymous);

        if (await middleware.TryAuthenticateWithJwt(_httpContext) &&
            await middleware.AuthorizeUserAsync(_httpContext, allowedRoles))
            // JWT authentication successful, proceed to authorization
            return await action();

        if (middleware.TryAuthenticateWithApiKey(_httpContext) &&
            await middleware.AuthorizeUserAsync(_httpContext, allowedRoles))
            // API key authentication successful, proceed to authorization
            return await action();

        // No valid authentication (JWT or API key), handle based on allowAnonymous
        if (allowAnonymous) return await action();
        _httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await _httpContext.Response.WriteAsync("Unauthorized: Missing or invalid credentials.");

        return _httpContext.Response.StatusCode switch
        {
            StatusCodes.Status401Unauthorized => new UnauthorizedObjectResult(
                "Unauthorized: Missing or invalid credentials."),
            StatusCodes.Status403Forbidden => new ForbidResult("Forbidden: Insufficient permissions."),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };
    }

    private Reevo.License.EntityFrameworkCore.Entities.License CreateAndSaveLicense(LicenseType licenseType, DateTime? expirationDate = null,
        string? hardwareId = null, int? maxActivations = null)
    {
        var productId = _dbContext.Products.First().ProductId;
        var licenseFeature = _dbContext.LicenseFeatures.First();
        var license = new DerivedLicense
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