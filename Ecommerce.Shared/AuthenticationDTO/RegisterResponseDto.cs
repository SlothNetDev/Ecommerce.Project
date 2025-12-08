namespace Ecommerce.Shared.AuthenticationDTO;

/// <summary>
/// Response DTO for user registration.
/// Indicates whether registration was successful and if email verification is required.
/// </summary>
public record RegisterResponseDto()
{
    /// <summary>
    /// The unique identifier of the newly created user account.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// The email address of the registered user.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Whether the user's email has been verified.
    /// For OTP-based registration, this will be false until verification is complete.
    /// </summary>
    public bool EmailVerified { get; init; }

    /// <summary>
    /// Human-readable message about the registration status.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// When the verification code expires (if email verification is required).
    /// </summary>
    public DateTime ExpiresAt { get; init; }
};
