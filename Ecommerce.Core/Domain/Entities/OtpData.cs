namespace Ecommerce.Core.Domain.Entities;

/// <summary>
/// Represents OTP (One-Time Password) data for email verification.
/// Contains all necessary information to manage OTP lifecycle.
/// </summary>
public class OtpData
{
    /// <summary>
    /// The actual 6-digit OTP code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// The email address associated with this OTP.
    /// Normalized to lowercase for consistency.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// When the OTP was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the OTP expires and becomes invalid.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Number of verification attempts made with this OTP.
    /// Used to prevent brute force attacks.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Whether this OTP has been successfully used for verification.
    /// Once used, the OTP should be removed from storage.
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Checks if the OTP is currently valid (not expired and not used).
    /// </summary>
    public bool IsValid => !IsUsed && DateTime.UtcNow <= ExpiresAt;

    /// <summary>
    /// Checks if the OTP has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}
