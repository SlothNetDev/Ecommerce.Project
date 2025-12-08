using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces.Register;

/// <summary>
/// Defines production-grade operations for secure user registration workflows,
/// including modern OTP-based email verification, registration, and password recovery features.
/// Ensures transactional integrity and proper user onboarding for authentication subsystems.
/// </summary>
public interface IUserRegistrationService
{
    /// <summary>
    /// Registers a new user in the system using the provided registration details.
    /// Uses modern OTP-based email verification instead of traditional confirmation links.
    /// </summary>
    /// <param name="request">The user registration details including email, password, and profile info.</param>
    /// <returns>
    /// A response indicating the result of the registration with OTP verification status.
    /// </returns>
    Task<ResponseType<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request);

    /// <summary>
    /// Verifies the user's email address using a 6-digit OTP code.
    /// Activates the account upon successful verification.
    /// </summary>
    /// <param name="request">The email verification request containing email and OTP code.</param>
    /// <returns>
    /// A response indicating whether the email verification was successful.
    /// </returns>
    Task<ResponseType<string>> VerifyEmailAsync(EmailVerificationRequestDto request);

    /// <summary>
    /// Legacy method for backward compatibility with token-based email confirmation.
    /// Now delegates to the new OTP-based verification workflow.
    /// </summary>
    /// <param name="token">The confirmation token sent to the user's email.</param>
    /// <param name="email">The email address to confirm.</param>
    /// <returns>
    /// A response indicating whether the email confirmation was successful or if there was an error.
    /// </returns>
    Task<ResponseType<string>> ConfirmEmail(string token, string email);

    /// <summary>
    /// Initiates the password reset process for a user who has forgotten their password.
    /// In production, this securely sends a reset link or code to the user's email.
    /// </summary>
    /// <param name="email">The email address associated with the user account.</param>
    /// <returns>
    /// A response indicating whether the password reset instructions were sent successfully.
    /// </returns>
    Task<ResponseType<string>> ForgotPassword(string email);
}