using Ecommerce.Shared.Enums;

namespace Ecommerce.Core.Application.Common.Interfaces.Register;

/// <summary>
/// Defines operations for One-Time Password (OTP) generation, validation, and management.
/// Used for secure email verification workflows in user registration and password recovery.
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Generates a new 6-digit OTP code for the specified email address.
    /// The OTP is stored temporarily with an expiration time for later validation.
    /// </summary>
    /// <param name="email">The email address for which to generate the OTP.</param>
    /// <returns>
    /// A task that returns true if the OTP was generated and stored successfully,
    /// false otherwise (e.g., if email is invalid or storage fails).
    /// </returns>
    Task<bool> GenerateOtpAsync(string email);

    /// <summary>
    /// Validates the provided OTP code against the stored OTP for the given email.
    /// This method handles OTP verification, expiration checking, and attempt tracking.
    /// </summary>
    /// <param name="email">The email address associated with the OTP.</param>
    /// <param name="otpCode">The 6-digit OTP code to validate.</param>
    /// <returns>
    /// A task that returns an OtpValidationResult indicating the validation outcome:
    /// - Valid: OTP is correct and not expired
    /// - Invalid: OTP is incorrect
    /// - Expired: OTP has expired
    /// - TooManyAttempts: User has exceeded maximum verification attempts
    /// - NotFound: No OTP found for this email
    /// </returns>
    Task<OtpValidationResult> ValidateOtpAsync(string email, string otpCode);

    /// <summary>
    /// Checks if an OTP exists and is still valid (not expired) for the given email.
    /// </summary>
    /// <param name="email">The email address to check.</param>
    /// <returns>
    /// True if a valid OTP exists for the email, false otherwise.
    /// </returns>
    Task<bool> HasValidOtpAsync(string email);

    /// <summary>
    /// Removes any stored OTP for the specified email address.
    /// Used when OTP verification is complete or when cleaning up expired codes.
    /// </summary>
    /// <param name="email">The email address for which to remove the OTP.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveOtpAsync(string email);
}

