using Ecommerce.Core.Application.Common.Interfaces.Register;
using Ecommerce.Core.Domain.Utilities;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Services.Notification;

/// <summary>
/// Service for generating, storing, and validating One-Time Passwords (OTP).
/// Uses in-memory storage for development; can be easily extended to use Redis or database.
/// </summary>
public class OtpService(ILogger<OtpService> logger) : IOtpService
{
    private readonly Dictionary<string, OtpData> _otpStore = new();
    private readonly TimeSpan _otpExpiration = TimeSpan.FromMinutes(10); // 10 minutes
    private readonly int _maxAttempts = 5; // Maximum verification attempts

    /// <inheritdoc/>
    public async Task<string> GenerateOtpAsync(string email)
    {
        // Normalize email to lowercase for consistency
        var normalizedEmail = email.ToLowerInvariant();

        // Generate a 6-digit random OTP
        var otpCode = OtpGenerator.GenerateOtp();

        // Create OTP data
        var otp = new OtpData
        {
            Code = otpCode,
            Email = normalizedEmail,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(_otpExpiration),
            Attempts = 0,
            IsUsed = false
        };

        // Store in memory (replace with Redis/database in production)
        _otpStore[normalizedEmail] = otp;

        // Clean up expired OTPs periodically
        await CleanupExpiredOtpsAsync();

        logger.LogInformation("OTP generated for email: {Email}, expires at: {ExpiresAt}",
            normalizedEmail, otp.ExpiresAt);

        return otp.Code;
    }

    /// <inheritdoc/>
    public async Task<OtpValidationResult> ValidateOtpAsync(string email, string otpCode)
    {
        var normalizedEmail = email.ToLowerInvariant();

        // Check if OTP exists
        if (!_otpStore.TryGetValue(normalizedEmail, out var otpData))
        {
            logger.LogWarning("OTP validation failed: No OTP found for email: {Email}", normalizedEmail);
            return await Task.FromResult(OtpValidationResult.NotFound);
        }

        // Check if already used
        if (otpData.IsUsed)
        {
            logger.LogWarning("OTP validation failed: OTP already used for email: {Email}", normalizedEmail);
            return await Task.FromResult(OtpValidationResult.Invalid);
        }

        // Check if expired
        if (DateTime.UtcNow > otpData.ExpiresAt)
        {
            logger.LogWarning("OTP validation failed: OTP expired for email: {Email}", normalizedEmail);
            return await Task.FromResult(OtpValidationResult.Expired);
        }

        // Check attempt limit
        if (otpData.Attempts >= _maxAttempts)
        {
            logger.LogWarning("OTP validation failed: Too many attempts for email: {Email}", normalizedEmail);
            return await Task.FromResult(OtpValidationResult.TooManyAttempts);
        }

        // Increment attempts
        otpData.Attempts++;

        // Validate code
        if (otpData.Code != otpCode)
        {
            logger.LogWarning("OTP validation failed: Invalid code for email: {Email}, attempt: {Attempt}",
                normalizedEmail, otpData.Attempts);

            // Remove OTP after max attempts
            if (otpData.Attempts >= _maxAttempts)
            {
                _otpStore.Remove(normalizedEmail);
            }

            return await Task.FromResult(OtpValidationResult.Invalid);
        }

        // Mark as used and remove from store
        otpData.IsUsed = true;
        _otpStore.Remove(normalizedEmail);

        logger.LogInformation("OTP validation successful for email: {Email}", normalizedEmail);
        return await Task.FromResult(OtpValidationResult.Valid);
    }

    /// <inheritdoc/>
    public Task<bool> HasValidOtpAsync(string email)
    {
        var normalizedEmail = email.ToLowerInvariant();

        if (!_otpStore.TryGetValue(normalizedEmail, out var otpData))
        {
            return Task.FromResult(false);
        }

        // Check if expired or used
        return Task.FromResult(!otpData.IsUsed && DateTime.UtcNow <= otpData.ExpiresAt);
    }

    /// <inheritdoc/>
    public Task RemoveOtpAsync(string email)
    {
        var normalizedEmail = email.ToLowerInvariant();
        _otpStore.Remove(normalizedEmail);
        logger.LogInformation("OTP removed for email: {Email}", normalizedEmail);
        
        return Task.CompletedTask;
    }

    
    /// <summary>
    /// Cleans up expired OTPs from memory to prevent memory leaks.
    /// In production, this would be handled differently (e.g., background job).
    /// </summary>
    private Task CleanupExpiredOtpsAsync()
    {
        var expiredEmails = _otpStore
            .Where(kvp => DateTime.UtcNow > kvp.Value.ExpiresAt)
            .Select(kvp => kvp.Key)
            .ToList();
        
        foreach (var email in expiredEmails)
        {
            _otpStore.Remove(email);
            logger.LogDebug("Cleaned up expired OTP for email: {Email}", email);
        }

        return Task.CompletedTask;
    }

    
}
