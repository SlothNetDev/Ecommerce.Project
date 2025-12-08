namespace Ecommerce.Shared.AuthenticationDTO;

/// <summary>
/// Request DTO for email verification using OTP code.
/// Used when users submit their verification code to confirm their email address.
/// </summary>
public record EmailVerificationRequestDto()
{
    /// <summary>
    /// The email address to verify.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// The 6-digit OTP verification code sent to the email address.
    /// </summary>
    public string VerificationCode { get; init; } = string.Empty;
};
