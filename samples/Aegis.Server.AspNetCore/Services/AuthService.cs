﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Aegis.Server.AspNetCore.Data.Context;
using Aegis.Server.AspNetCore.DTOs;
using Aegis.Server.AspNetCore.Entities;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Volo.Abp.Application.Services;

namespace Aegis.Server.AspNetCore.Services;

public class AuthService(ApplicationDbContext dbContext, IOptions<JwtSettings> options) : ApplicationService, IAuthService
{
    public async Task<JwtTokenDto?> LoginUserAsync(LoginDto login)
    {
        var user = await dbContext.Users.Where(x => x.Username == login.Username).FirstOrDefaultAsync();
        if (user == null || !VerifyPassword(login.Password, user.PasswordHash))
        {
            return null;
        }

        var token = await GenerateJwtToken(user.Id, user.Username, user.Role);

        return token;
    }

    public async Task<bool> RegisterAsync(RegisterDto newUser)
    {
        if (newUser.Password != newUser.ConfirmPassword ||
            await dbContext.Users.AnyAsync(x => x.Username == newUser.Username) ||
            await dbContext.Users.AnyAsync(x => x.Email == newUser.Email) ||
            !await dbContext.Roles.AnyAsync(x => x.Name == newUser.Role)) return false;

        var user = new User
        {
            Username = newUser.Username,
            Email = newUser.Email,
            PasswordHash = HashPassword(newUser.Password),
            Role = newUser.Role,
            FullName = newUser.FullName
        };

        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<JwtTokenDto> GenerateJwtToken(Guid userId, string userName, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = SHA256.HashData(Encoding.ASCII.GetBytes(options.Value.Secret));
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Name, userName),
                new(ClaimTypes.Role, role)
            }),
            Expires = DateTime.UtcNow.AddDays(options.Value.AccessTokenExpirationInDays),
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var accessToken = tokenHandler.CreateToken(tokenDescriptor);
        var refreshToken = GenerateRefreshToken();
        var refreshExpirationDate = DateTime.UtcNow.AddDays(options.Value.RefreshTokenExpirationInDays);

        
        // Check if the user already has a refresh token
        var existingRefreshToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.UserId == userId);
        if (existingRefreshToken != null)
        {
            // Update the existing refresh token
            existingRefreshToken.Token = refreshToken;
            existingRefreshToken.Expires = refreshExpirationDate;
            existingRefreshToken.Role = role; 
        }
        else
        {
            // Add a new refresh token
            existingRefreshToken = (await dbContext.RefreshTokens.AddAsync(new RefreshToken
            {
                UserId = userId,
                Token = refreshToken,
                Expires = refreshExpirationDate,
                Role = role
            })).Entity;
        }
        
        await dbContext.SaveChangesAsync();

        var user = await dbContext.Users.Where(x => x.Id == userId).FirstOrDefaultAsync();
        if (user != null)
        {
            user.RefreshTokenId = existingRefreshToken.Id;
            user.RefreshToken = existingRefreshToken;
            await dbContext.SaveChangesAsync();
        }

        return new JwtTokenDto
        {
            AccessToken = tokenHandler.WriteToken(accessToken),
            AccessTokenExpiration = tokenDescriptor.Expires.Value,
            RefreshToken = refreshToken,
            RefreshTokenExpiration = refreshExpirationDate
        };
    }

    public Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = SHA256.HashData(Encoding.ASCII.GetBytes(options.Value.Secret));
            var claimsPrincipal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero,
                ValidateLifetime = true
            }, out _);

            return Task.FromResult(claimsPrincipal)!;
        }
        catch
        {
            return Task.FromResult<ClaimsPrincipal>(null!)!;
        }
    }

    #region Private

    /// <summary>
    ///     Generates a random refresh token.
    /// </summary>
    /// <returns>The generated refresh token.</returns>
    internal string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    ///     Hashes a password using PBKDF2 with HMACSHA256.
    /// </summary>
    /// <param name="password">The password to hash.</param>
    /// <returns>The hashed password as a base64-encoded string.</returns>
    internal string HashPassword(string password)
    {
        return Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password,
            Encoding.ASCII.GetBytes(options.Value.Salt),
            KeyDerivationPrf.HMACSHA256,
            10000,
            256 / 8));
    }

    /// <summary>
    ///     Verifies a password against a hashed password.
    /// </summary>
    /// <param name="password">The password to verify.</param>
    /// <param name="hashedPassword">The hashed password to compare against.</param>
    /// <returns>True if the passwords match, false otherwise.</returns>
    internal bool VerifyPassword(string password, string hashedPassword)
    {
        return HashPassword(password) == hashedPassword;
    }

    #endregion
}