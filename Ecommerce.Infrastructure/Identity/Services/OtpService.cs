using Ecommerce.Core.Application.Common.Interfaces.Register;
using Ecommerce.Core.Domain.Utilities;
using Ecommerce.Shared.Enums;
using Hangfire;
using Microsoft.Extensions.Logging;
using OtpData = Ecommerce.Core.Domain.Entities.OtpData;

namespace Ecommerce.Infrastructure.Identity.Services;

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
    public async Task<bool> GenerateOtpAsync(string email)
    {
        try
        {
            // Normalize email to lowercase for consistency
            var normalizedEmail = email.ToLowerInvariant();

            // Generate a 6-digit random OTP using utility class
            var otpCode = OtpGenerator.GenerateOtp();

            // Create OTP data
            var otpData = new OtpData
            {
                Code = otpCode,
                Email = normalizedEmail,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(_otpExpiration),
                Attempts = 0,
                IsUsed = false
            };

            // Store in memory (replace with Redis/database in production)
            _otpStore[normalizedEmail] = otpData;

            // Schedule cleanup with Hangfire (fire-and-forget, runs after expiration)
            BackgroundJob.Schedule(
                () => CleanupExpiredOtp(normalizedEmail),
                _otpExpiration);

            logger.LogInformation("OTP generated for email: {Email}, expires at: {ExpiresAt}",
                normalizedEmail, otpData.ExpiresAt);

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate OTP for email: {Email}", email);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<OtpValidationResult> ValidateOtpAsync(string email, string otpCode)
    {
        try
        {
            var normalizedEmail = email.ToLowerInvariant();

            // Check if OTP exists
            if (!_otpStore.TryGetValue(normalizedEmail, out var otpData))
            {
                logger.LogWarning("OTP validation failed: No OTP found for email: {Email}", normalizedEmail);
                return OtpValidationResult.NotFound;
            }

            // Check if already used
            if (otpData.IsUsed)
            {
                logger.LogWarning("OTP validation failed: OTP already used for email: {Email}", normalizedEmail);
                return OtpValidationResult.Invalid;
            }

            // Check if expired
            if (DateTime.UtcNow > otpData.ExpiresAt)
            {
                logger.LogWarning("OTP validation failed: OTP expired for email: {Email}", normalizedEmail);
                return OtpValidationResult.Expired;
            }

            // Check attempt limit
            if (otpData.Attempts >= _maxAttempts)
            {
                logger.LogWarning("OTP validation failed: Too many attempts for email: {Email}", normalizedEmail);
                return OtpValidationResult.TooManyAttempts;
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

                return OtpValidationResult.Invalid;
            }

            // Mark as used and remove from store
            otpData.IsUsed = true;
            _otpStore.Remove(normalizedEmail);

            logger.LogInformation("OTP validation successful for email: {Email}", normalizedEmail);
            return await Task.FromResult(OtpValidationResult.Valid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OTP validation error for email: {Email}", email);
            return OtpValidationResult.Invalid;
        }
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
    /// Cleans up a specific expired OTP (called by Hangfire background job).
    /// This method is designed to be called asynchronously by Hangfire after OTP expiration.
    /// </summary>
    /// <param name="email">The email address associated with the OTP to clean up.</param>
    [AutomaticRetry(Attempts = 3)]
    public Task CleanupExpiredOtp(string email)
    {
        try
        {
            var normalizedEmail = email.ToLowerInvariant();

            if (_otpStore.TryGetValue(normalizedEmail, out var otpData))
            {
                if (otpData.IsExpired || otpData.IsUsed)
                {
                    _otpStore.Remove(normalizedEmail);
                    logger.LogInformation("Cleaned up expired/used OTP for email: {Email}", normalizedEmail);
                }
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cleaning up OTP for email: {Email}", email);
            throw; // Let Hangfire handle retry logic
        }
    }
    
}
