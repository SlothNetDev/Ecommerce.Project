namespace Ecommerce.Infrastructure.Identity.Entities;

/// <summary>
/// Internal class to store OTP data.
/// In production, this would be stored in Redis or database.
/// </summary>
public class OtpData
{
    public string Code { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public bool IsUsed { get; set; }
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt; //expression embodied property
}