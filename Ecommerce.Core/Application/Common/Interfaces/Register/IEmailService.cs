namespace Ecommerce.Core.Application.Common.Interfaces.Register;

/// <summary>
/// Defines operations for sending various types of emails in the application.
/// Provides a clean abstraction for email delivery across different providers (SMTP, SendGrid, etc.).
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an OTP (One-Time Password) verification email to the specified email address.
    /// Used during user registration and other verification workflows.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="otpCode">The 6-digit OTP code to include in the email.</param>
    /// <param name="userName">The user's name for personalization (optional).</param>
    /// <returns>
    /// A task that returns true if the email was sent successfully, false otherwise.
    /// </returns>
    Task<bool> SendOtpEmailAsync(string email, string otpCode, string? userName = null);

    /// <summary>
    /// Sends a welcome email to newly registered users after successful email verification.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="userName">The user's name for personalization.</param>
    /// <param name="firstName">The user's first name for personalization.</param>
    /// <returns>
    /// A task that returns true if the email was sent successfully, false otherwise.
    /// </returns>
    Task<bool> SendWelcomeEmailAsync(string email, string userName, string firstName);

    /// <summary>
    /// Sends a password reset email with a secure reset link or OTP.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="resetToken">The password reset token or OTP code.</param>
    /// <param name="userName">The user's name for personalization.</param>
    /// <returns>
    /// A task that returns true if the email was sent successfully, false otherwise.
    /// </returns>
    Task<bool> SendPasswordResetEmailAsync(string email, string resetToken, string userName);
}


