namespace Ecommerce.Infrastructure.Identity.Entities;

public class ApplicationToken
{
    public string TokenId { get; set; } // Primary key
    public string Token { get; set; } = string.Empty; // Actual refresh token string
    public DateTime Expires { get; set; } // Expiration date
    public bool IsExpired => DateTime.UtcNow >= Expires; //expression embodied property
         
    public DateTime Created { get; set; }
    public string CreatedByIp { get; set; } = string.Empty;
         
    public DateTime? Revoked { get; set; }
    public string? RevokedByIp { get; set; }
         
    public bool IsActive => Revoked == null && !IsExpired; //returns true if both was true return true
    
    // Add reason for revocation
    public string? RevocationReason { get; set; } // "UserLogout", "SuspiciousActivity", etc.
    // Navigation
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
}