using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces;

/// <summary>
/// Defines production-grade operations for secure user registration workflows, 
/// including registration, email confirmation, and password recovery features. 
/// Ensures transactional integrity and proper user onboarding for authentication subsystems.
/// </summary>
public interface IUserRegistrationService
{
    /// <summary>
    /// Registers a new user in the system using the provided registration details.
    /// Intended for production environments with full input validation and audit logging.
    /// </summary>
    /// <param name="request">The user registration details including email, password, and profile info.</param>
    /// <returns>
    /// A response indicating the result of the registration (success, failure, or validation errors).
    /// </returns>
    Task<ResponseType<string>> Register(RegisterRequestDto request);

    /// <summary>
    /// Confirms the user's email address during account activation.
    /// Used in production flows to verify email ownership with a secure token.
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