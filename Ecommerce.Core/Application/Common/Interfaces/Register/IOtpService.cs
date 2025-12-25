using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Shared.Enums;
using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces.Register;

/// <summary>
/// Defines operations for One-Time Password (OTP) generation, validation, and management.
/// Used for secure email verification workflows in user registration and password recovery.
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Creates a new OTP for the specified user and stores it in the database.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<ResponseType<string>> CreateEmailOtpAsync(Guid userId);
    
    /// <summary>
    /// Verifies the specified OTP code for the specified user.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="otpCode"></param>
    /// <returns></returns>
    Task<OtpValidationResult> VerifyEmailOtpAsync(Guid userId, string otpCode);
    
    /// <summary>
    /// Validating Otp by using userId
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<ResponseType<bool>> HasValidOtpAsync(Guid userId);
    
    /// <summary>
    /// Resend Otp method if ever otp was expired
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<ResponseType<string>> ResendOtpAsync(Guid userId);
    
}

