using System.ComponentModel.DataAnnotations;
using Reevo.License.EntityFrameworkCore.Entities;

namespace Sample.License.Web.Entities;

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
    public ICollection<DerivedLicense> Licenses { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
    public int RefreshTokenId { get; set; }
    public RefreshToken RefreshToken { get; set; }
}