namespace Ecommerce.Shared.Enums;

/// <summary>
/// Represents the result of an OTP validation attempt.
/// </summary>
public enum OtpValidationResult
{
    /// <summary>
    /// The OTP is valid and correct.
    /// </summary>
    Valid,

    /// <summary>
    /// The OTP code is incorrect.
    /// </summary>
    Invalid,

    /// <summary>
    /// The OTP has expired and is no longer valid.
    /// </summary>
    Expired,

    /// <summary>
    /// The user has exceeded the maximum number of verification attempts.
    /// </summary>
    TooManyAttempts,

    /// <summary>
    /// No OTP was found for the provided email address.
    /// </summary>
    NotFound
}
