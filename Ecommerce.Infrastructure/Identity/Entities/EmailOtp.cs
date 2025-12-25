using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Infrastructure.Identity.Entities;

/// <summary>
/// Represents a one-time password (OTP) sent through email for user authentication purposes.
/// This entity stores the OTP details along with metadata regarding its expiration, verification, and usage attempts.
/// </summary>
public class EmailOtp
{
    public Guid EmailOtpId { get; set; } = Guid.NewGuid();
   
    //relationship with user
    public Guid UserId { get; set; }
    public ApplicationUser ApplicationUser { get; set; } = null!;
    
    //OtpData
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    
    //Meta Data
    public int Attempts { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    //Anti abuse
    public DateTime LastSentAt { get; set; }
    public int ResentCount { get; set; }
    
    public bool IsLocked => Attempts >= 5;
    public bool IsActive => !IsExpired && !IsLocked;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt; //expression embodied property
}