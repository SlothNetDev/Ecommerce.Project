namespace Ecommerce.Shared.TokenDTO;

/// <summary>
/// Create refresh token request
/// </summary>
public record ApplicationTokenDto
{
    public string TokenId { get; init; } // Primary key
    public string Token { get; init; } = string.Empty; // Actual refresh token string
    public DateTime Expires { get; init; } // Expiration date
    public bool IsExpired => DateTime.UtcNow >= Expires; //expression embodied property     
    public DateTime Created { get; init; }
    public string CreatedByIp { get; init; } = string.Empty;
         
    public DateTime? Revoked { get; init; }
    public string? RevokedByIp { get; init; }
    public bool IsActive => Revoked == null && !IsExpired; //returns true if both was true return true
    // Add reason for revocation
    public string? RevocationReason { get; init; } // "UserLogout", "SuspiciousActivity", etc.
    
    public Guid UserId { get; set; }
}