using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Sample.License.Web.Data.Context;
using Sample.License.Web.DTOs;
using Sample.License.Web.Entities;
using Sample.License.Web.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Sample.License.Web.Tests.Services;

public class AuthServiceTests
{
    #region Fields

    private readonly AuthService _authService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IOptions<JwtSettings> _jwtSettings;

    #endregion

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _jwtSettings = Options.Create(new JwtSettings
        {
            Secret = "testsecret",
            Salt = "testsalt",
            AccessTokenExpirationInDays = 1,
            RefreshTokenExpirationInDays = 7
        });

        _authService = new AuthService(_dbContext, _jwtSettings);
    }

    #region GenerateRefreshToken

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueToken()
    {
        // Act
        var token1 = _authService.GenerateRefreshToken();
        var token2 = _authService.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
    }

    #endregion

    #region LoginUserAsync

    [Fact]
    public async Task LoginUserAsync_ValidCredentials_ReturnsJwtTokenDto()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            PasswordHash = _authService.HashPassword("testpassword"), // Hash the password for testing
            Role = "User"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _authService.LoginUserAsync(new LoginDto
        {
            Username = "testuser",
            Password = "testpassword"
        });

        // Assert
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.AccessTokenExpiration.Should().BeAfter(DateTime.UtcNow);
        result.RefreshTokenExpiration.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginUserAsync_InvalidUsername_ReturnsNull()
    {
        // Act
        var result = await _authService.LoginUserAsync(new LoginDto
        {
            Username = "nonexistentuser",
            Password = "testpassword"
        });

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginUserAsync_InvalidPassword_ReturnsNull()
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

        // Act
        var result = await _authService.LoginUserAsync(new LoginDto
        {
            Username = "testuser",
            Password = "wrongpassword"
        });

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region RegisterAsync

    [Fact]
    public async Task RegisterAsync_UniqueCredentials_ReturnsTrueAndSavesUser()
    {
        // Arrange
        await _dbContext.Roles.AddAsync(new Role { Name = "User" });
        await _dbContext.SaveChangesAsync();
        var newUser = new RegisterDto
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "newpassword",
            ConfirmPassword = "newpassword",
            FullName = "New User",
            Role = "User"
        };

        // Act
        var result = await _authService.RegisterAsync(newUser);

        // Assert
        result.Should().BeTrue();
        var savedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == newUser.Username);
        savedUser.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterAsync_ExistingUsername_ReturnsFalse()
    {
        // Arrange
        var existingUser = new User
        {
            Username = "existinguser", 
            Email = "existing@example.com"
        };
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var newUser = new RegisterDto
        {
            Username = "existinguser", 
            Email = "newuser@example.com"
        };

        // Act
        var result = await _authService.RegisterAsync(newUser);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterAsync_ExistingEmail_ReturnsFalse()
    {
        // Arrange
        var existingUser = new User
        {
            Username = "existinguser", 
            Email = "existing@example.com"
        };
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var newUser = new RegisterDto { Username = "newuser", Email = "existing@example.com" };

        // Act
        var result = await _authService.RegisterAsync(newUser);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GenerateJwtToken

    [Fact]
    public async Task GenerateJwtToken_ReturnsValidJwtTokenDto()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _authService.GenerateJwtToken(userId, "testuser", "User");

        // Assert
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.AccessTokenExpiration.Should().BeAfter(DateTime.UtcNow);
        result.RefreshTokenExpiration.Should().BeAfter(DateTime.UtcNow);

        // Verify token claims
        var claims = await _authService.ValidateTokenAsync(result.AccessToken);
        var claimsDict = claims?.Claims.ToDictionary(claim => claim.Type, claim => claim.Value);
        claimsDict.Should().NotBeNullOrEmpty();
        claimsDict![ClaimTypes.NameIdentifier].Should().Be(userId.ToString());
        claimsDict[ClaimTypes.Name].Should().Be("testuser");
        claimsDict[ClaimTypes.Role].Should().Be("User");
    }

    [Fact]
    public async Task GenerateJwtToken_SavesRefreshTokenToDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _authService.GenerateJwtToken(userId, "testuser", "User");

        // Assert
        var refreshToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == userId);
        refreshToken.Should().NotBeNull();
    }

    #endregion

    #region ValidateTokenAsync Tests

    [Fact]
    public async Task ValidateTokenAsync_ValidToken_ReturnsClaimsPrincipal()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = (await _authService.GenerateJwtToken(userId, "testuser", "User")).AccessToken;

        // Act
        var result = await _authService.ValidateTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        var claimsDict = result!.Claims.ToDictionary(claim => claim.Type, claim => claim.Value);
        claimsDict.Should().NotBeNullOrEmpty();
        claimsDict![ClaimTypes.NameIdentifier].Should().Be(userId.ToString());
        claimsDict[ClaimTypes.Name].Should().Be("testuser");
        claimsDict[ClaimTypes.Role].Should().Be("User");
    }

    [Fact]
    public async Task ValidateTokenAsync_ExpiredToken_ThrowsException()
    {
        // Arrange
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.Value.Secret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }),
            Expires = DateTime.UtcNow.AddMinutes(-1), // Expired token
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        // Act
        var act = async () => await _authService.ValidateTokenAsync(tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor)));

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task ValidateTokenAsync_InvalidToken_ReturnsNull()
    {
        // Arrange
        const string invalidToken = "invalidtoken";

        // Act
        var result = await _authService.ValidateTokenAsync(invalidToken);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}