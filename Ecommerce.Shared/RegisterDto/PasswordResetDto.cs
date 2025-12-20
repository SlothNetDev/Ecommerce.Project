namespace Ecommerce.Shared.RegisterDto;

/// <summary>
/// Metadata returned for password reset requests (production-grade).
/// Includes expiration and optional background job ID for observability.
/// </summary>
public record PasswordResetRequestDto
{
    /// <summary>
    /// The email address the password reset is for.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// When the OTP or reset token expires.
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Optional background job ID (e.g., Hangfire) for monitoring email delivery.
    /// </summary>
    public string? BackgroundJobId { get; init; }
}
