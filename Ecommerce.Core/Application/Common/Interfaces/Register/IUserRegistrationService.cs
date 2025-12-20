using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.RegisterDto;
using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces.Register;

/// <summary>
/// Production-grade service for secure user registration workflows.
/// Handles OTP-based email verification, password reset, and onboarding.
/// Ensures transactional integrity, background job orchestration, and observability.
/// </summary>
public interface IUserRegistrationService
{
    /// <summary>
    /// Registers a new user in the system.
    /// - Creates the account with EmailConfirmed = false
    /// - Generates a 6-digit OTP for verification
    /// - Enqueues OTP email in the background
    /// </summary>
    /// <param name="request">Registration details including email, password, and profile info.</param>
    /// <returns>
    /// Response indicating registration success.
    /// OTP is sent asynchronously via background job; never exposed in the response.
    /// Optionally includes metadata for monitoring (job ID, OTP expiry).
    /// </returns>
    Task<ResponseType<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request);

    /// <summary>
    /// Verifies a user's email using a 6-digit OTP code.
    /// On success, activates the account and optionally sends a welcome email.
    /// </summary>
    /// <param name="request">Email verification request containing email and OTP code.</param>
    /// <returns>
    /// Response indicating whether verification succeeded or failed,
    /// including detailed failure reasons (expired, invalid, too many attempts).
    /// </returns>
    Task<ResponseType<string>> VerifyEmailAsync(EmailVerificationRequestDto request);

    /// <summary>
    /// Initiates a password reset workflow for a user who has forgotten their password.
    /// - Generates a secure reset token or OTP
    /// - Stores it securely with expiration
    /// - Enqueues reset email in background
    /// </summary>
    /// <param name="email">Email address associated with the user account.</param>
    /// <returns>
    /// Response containing metadata about the password reset request,
    /// e.g., token expiry, background job ID (if available).
    /// </returns>
    Task<ResponseType<PasswordResetRequestDto>> ForgotPasswordAsync(string email);

    /// <summary>
    /// Legacy token-based email confirmation method.
    /// Deprecated in favor of OTP-based verification.
    /// </summary>
    /// <param name="token">Legacy confirmation token.</param>
    /// <param name="email">Email address associated with the token.</param>
    /// <returns>Failure response indicating method is deprecated.</returns>
    [Obsolete("Please use VerifyEmailAsync for OTP-based email verification.")]
    Task<ResponseType<string>> ConfirmEmailAsync(string token, string email);
}

