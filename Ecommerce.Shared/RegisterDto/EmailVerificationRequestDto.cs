namespace Ecommerce.Shared.RegisterDto;

/// <summary>
/// Request DTO for email verification using OTP code.
/// Used when users submit their verification code to confirm their email address.
/// </summary>
public record EmailVerificationRequestDto()
{
    public Guid UserId  { get; init; }
    public string Email { get; init; } = string.Empty;
    public string VerificationCode { get; init; } = string.Empty;
};
