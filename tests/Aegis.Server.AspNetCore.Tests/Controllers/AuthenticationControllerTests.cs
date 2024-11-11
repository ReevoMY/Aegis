using System.IdentityModel.Tokens.Jwt;
using Aegis.Server.AspNetCore.Controllers;
using Aegis.Server.AspNetCore.Data.Context;
using Aegis.Server.AspNetCore.DTOs;
using Aegis.Server.AspNetCore.Entities;
using Aegis.Server.AspNetCore.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Aegis.Server.AspNetCore.Tests.Controllers;

public class AuthenticationControllerTests
{
    #region Fields

    private readonly AuthService _authService;
    private readonly AuthenticationController _controller;
    private readonly ApplicationDbContext _dbContext;

    #endregion

    // Constructor for setting up the test environment
    public AuthenticationControllerTests()
    {
        // 1. Set up an in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);

        // 2. Configure JWT settings for testing
        var jwtSettings = Options.Create(new JwtSettings
        {
            Secret = "ThisIsMyVerySecretKeyForJWT!",
            Salt = "SaltForPasswordHashing",
            AccessTokenExpirationInDays = 1,
            RefreshTokenExpirationInDays = 7
        });

        // 3. Create instances of AuthService and AuthenticationController for testing
        _authService = new AuthService(_dbContext, jwtSettings);
        _controller = new AuthenticationController(_authService, _dbContext);
    }

    #region Register

    [Fact]
    public async Task Register_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        await _dbContext.Roles.AddAsync(new Role { Name = "User" });
        await _dbContext.SaveChangesAsync();
        var registerDto = new RegisterDto
        {
            Username = "testuser",
            Email = "testuser@example.com",
            Password = "testpassword",
            ConfirmPassword = "testpassword",
            FullName = "Test User",
            Role = "User"
        };

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult!.Value.Should().Be("User registered successfully.");

        // Verify user is saved in the database
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == registerDto.Username);
        user.Should().NotBeNull();
        user!.Email.Should().Be(registerDto.Email);
        _authService.VerifyPassword(registerDto.Password, user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Register_ExistingUsername_ReturnsBadRequest()
    {
        // Arrange
        var existingUser = new User
        {
            Username = "existinguser",
            Email = "existing@example.com",
            PasswordHash = _authService.HashPassword("password")
        };
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var registerDto = new RegisterDto
        {
            Username = "existinguser",
            Email = "newuser@example.com",
            Password = "newpassword",
            ConfirmPassword = "newpassword"
        };

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult!.Value.Should().Be("Username or email is already taken or role does not exist.");
    }

    [Fact]
    public async Task Register_ExistingEmail_ReturnsBadRequest()
    {
        // Arrange
        var existingUser = new User
        {
            Username = "existinguser",
            Email = "existing@example.com",
            PasswordHash = _authService.HashPassword("password")
        };
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var registerDto = new RegisterDto
        {
            Username = "newuser",
            Email = "existing@example.com",
            Password = "newpassword",
            ConfirmPassword = "newpassword"
        };

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult!.Value.Should().Be("Username or email is already taken or role does not exist.");
    }

    [Fact]
    public async Task Register_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "testuser",
            Email = "invalid email",
            Password = "testpassword",
            ConfirmPassword = "testpassword",
            FullName = "Test User",
            Role = "User"
        };
        var expectedError = new KeyValuePair<string, string>("Email", "Invalid email format.");
        _controller.ModelState.AddModelError(expectedError.Key, expectedError.Value);

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errors = badRequestResult!.Value.Should().BeOfType<SerializableError>().Subject;
        errors.Count.Should().Be(1);
        (errors[expectedError.Key] as string[])![0].Should().Be(expectedError.Value);
    }

    #endregion

    #region Login

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkResultWithJwtToken()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            PasswordHash = _authService.HashPassword("testpassword"),
            Role = "User"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        var loginDto = new LoginDto { Username = "testuser", Password = "testpassword" };

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jwtTokenDto = okResult.Value.Should().BeOfType<JwtTokenDto>().Subject;
        jwtTokenDto.Should().NotBeNull();
        jwtTokenDto.AccessToken.Should().NotBeNullOrEmpty();
        jwtTokenDto.RefreshToken.Should().NotBeNullOrEmpty();

        // Verify token claims
        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.ReadToken(jwtTokenDto.AccessToken) as JwtSecurityToken;
        securityToken.Should().NotBeNull();
        var claims = securityToken!.Claims.ToDictionary((claim) => claim.Type, claim => claim.Value);
        claims[JwtRegisteredClaimNames.NameId].Should().Be(user.Id.ToString());
        claims[JwtRegisteredClaimNames.UniqueName].Should().Be(user.Username);
        claims["role"].Should().Be(user.Role);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser", 
            PasswordHash = _authService.HashPassword("testpassword"), 
            Role = "User"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var loginDto = new LoginDto { Username = "testuser", Password = "wrongpassword" };

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().Be("Invalid username or password.");
    }

    #endregion

    #region RefreshToken

    [Fact]
    public async Task RefreshToken_ValidRefreshToken_ReturnsOkResultWithNewTokens()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser", 
            PasswordHash = _authService.HashPassword("testpassword"), 
            Role = "User"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Generate initial tokens
        var initialTokens = await _authService.GenerateJwtToken(user.Id, user.Username, user.Role);
        var refreshToken = new RefreshTokenDto { Token = initialTokens.RefreshToken };
        await Task.Delay(1000); // Delay to ensure new tokens are different

        // Act
        var result = await _controller.RefreshToken(refreshToken);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var newTokens = okResult.Value.Should().BeOfType<JwtTokenDto>().Subject;
        newTokens.Should().NotBeNull();
        newTokens.AccessToken.Should().NotBeNullOrEmpty();
        newTokens.AccessToken.Should().NotBe(initialTokens.AccessToken);
        newTokens.RefreshToken.Should().NotBeNullOrEmpty();
        newTokens.RefreshToken.Should().NotBe(initialTokens.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_InvalidRefreshToken_ReturnsUnauthorized()
    {
        // Arrange
        var refreshToken = new RefreshTokenDto { Token = "invalid-refresh-token" };

        // Act
        var result = await _controller.RefreshToken(refreshToken);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().Be("Invalid refresh token.");
    }

    [Fact]
    public async Task RefreshToken_MissingUser_ReturnsUnauthorized()
    {
        // Arrange
        var refreshToken = new RefreshTokenDto { Token = "some-refresh-token" };
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = Guid.NewGuid(), // Non-existing user
            Token = refreshToken.Token,
            Expires = DateTime.UtcNow.AddDays(7)
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.RefreshToken(refreshToken);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().Be("User associated with refresh token not found.");
    }

    [Fact]
    public async Task RefreshToken_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var refreshToken = new RefreshTokenDto { Token = "invalid-refresh-token" };
        var expectedError = new KeyValuePair<string, string>("Token", "Invalid refresh token.");
        _controller.ModelState.AddModelError(expectedError.Key, expectedError.Value);

        // Act
        var result = await _controller.RefreshToken(refreshToken);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errors = badRequestResult!.Value.Should().BeOfType<SerializableError>().Subject;
        errors.Count.Should().Be(1);
        (errors[expectedError.Key] as string[])![0].Should().Be(expectedError.Value);
    }

    #endregion
}