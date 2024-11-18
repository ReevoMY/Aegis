using Aegis.Server.AspNetCore.DTOs;
using System.Security.Claims;
using Volo.Abp.Application.Services;

namespace Aegis.Server.AspNetCore.Services;

public interface IAuthService : IApplicationService
{
    /// <summary>
    ///     Authenticates a user and generates a JWT token if successful.
    /// </summary>
    /// <param name="login">The login credentials.</param>
    /// <returns>The JWT token DTO containing the access token, and expiration time, or null if authentication failed.</returns>
    Task<JwtTokenDto?> LoginUserAsync(LoginDto login);

    /// <summary>
    ///     Registers a new user.
    /// </summary>
    /// <param name="newUser">The new user registration details.</param>
    /// <returns>True if registration was successful, false otherwise.</returns>
    Task<bool> RegisterAsync(RegisterDto newUser);

    /// <summary>
    ///     Generates a JWT and refresh token for a given user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="userName">The username of the user.</param>
    /// <param name="role">The role of the user.</param>
    /// <returns>The JWT token DTO containing the access token, refresh token, and expiration times.</returns>
    Task<JwtTokenDto> GenerateJwtToken(Guid userId, string userName, string role);

    /// <summary>
    ///     Validates a JWT token and returns the associated ClaimsPrincipal if valid.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>The ClaimsPrincipal associated with the token, or null if the token is invalid.</returns>
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);
}