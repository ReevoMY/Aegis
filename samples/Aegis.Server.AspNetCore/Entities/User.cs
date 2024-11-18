﻿using System.ComponentModel.DataAnnotations;
using Aegis.Server.Entities;

namespace Aegis.Server.AspNetCore.Entities;

public class User
{
    public Guid Id { get; set; }

    [StringLength(30)] public string Username { get; set; } = string.Empty;

    [StringLength(50)] public string FullName { get; set; } = string.Empty;

    [StringLength(50)] public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;

    // Navigation Properties
    public ICollection<MyLicense> Licenses { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
    public int RefreshTokenId { get; set; }
    public RefreshToken RefreshToken { get; set; }
}