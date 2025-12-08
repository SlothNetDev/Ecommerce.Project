using Ecommerce.Core.Application.Common.Interfaces.Register;
using Ecommerce.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Identity.Services.Register;

/// <summary>
/// Service for generating, storing, and validating One-Time Passwords (OTP).
/// Uses in-memory storage for development; can be easily extended to use Redis or database.
/// </summary>
public class OtpService : IOtpService
{
    private readonly ILogger<OtpService> _logger;
    private readonly Dictionary<string, OtpData> _otpStore = new();
    private readonly TimeSpan _otpExpiration = TimeSpan.FromMinutes(10); // 10 minutes
    private readonly int _maxAttempts = 5; // Maximum verification attempts

    public OtpService(ILogger<OtpService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> GenerateOtpAsync(string email)
    {
        try
        {
            // Normalize email to lowercase for consistency
            var normalizedEmail = email.ToLowerInvariant();

            // Generate a 6-digit random OTP
            var otpCode = GenerateRandomOtp();

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

            // Clean up expired OTPs periodically
            await CleanupExpiredOtpsAsync();

            _logger.LogInformation("OTP generated for email: {Email}, expires at: {ExpiresAt}",
                normalizedEmail, otpData.ExpiresAt);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate OTP for email: {Email}", email);
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
                _logger.LogWarning("OTP validation failed: No OTP found for email: {Email}", normalizedEmail);
                return OtpValidationResult.NotFound;
            }

            // Check if already used
            if (otpData.IsUsed)
            {
                _logger.LogWarning("OTP validation failed: OTP already used for email: {Email}", normalizedEmail);
                return OtpValidationResult.Invalid;
            }

            // Check if expired
            if (DateTime.UtcNow > otpData.ExpiresAt)
            {
                _logger.LogWarning("OTP validation failed: OTP expired for email: {Email}", normalizedEmail);
                return OtpValidationResult.Expired;
            }

            // Check attempt limit
            if (otpData.Attempts >= _maxAttempts)
            {
                _logger.LogWarning("OTP validation failed: Too many attempts for email: {Email}", normalizedEmail);
                return OtpValidationResult.TooManyAttempts;
            }

            // Increment attempts
            otpData.Attempts++;

            // Validate code
            if (otpData.Code != otpCode)
            {
                _logger.LogWarning("OTP validation failed: Invalid code for email: {Email}, attempt: {Attempt}",
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

            _logger.LogInformation("OTP validation successful for email: {Email}", normalizedEmail);
            return OtpValidationResult.Valid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OTP validation error for email: {Email}", email);
            return OtpValidationResult.Invalid;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HasValidOtpAsync(string email)
    {
        var normalizedEmail = email.ToLowerInvariant();

        if (!_otpStore.TryGetValue(normalizedEmail, out var otpData))
        {
            return false;
        }

        // Check if expired or used
        return !otpData.IsUsed && DateTime.UtcNow <= otpData.ExpiresAt;
    }

    /// <inheritdoc/>
    public async Task RemoveOtpAsync(string email)
    {
        var normalizedEmail = email.ToLowerInvariant();
        _otpStore.Remove(normalizedEmail);
        _logger.LogInformation("OTP removed for email: {Email}", normalizedEmail);
    }

    /// <summary>
    /// Generates a random 6-digit OTP code.
    /// </summary>
    private static string GenerateRandomOtp()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    /// <summary>
    /// Cleans up expired OTPs from memory to prevent memory leaks.
    /// In production, this would be handled differently (e.g., background job).
    /// </summary>
    private async Task CleanupExpiredOtpsAsync()
    {
        var expiredEmails = _otpStore
            .Where(kvp => DateTime.UtcNow > kvp.Value.ExpiresAt)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var email in expiredEmails)
        {
            _otpStore.Remove(email);
            _logger.LogDebug("Cleaned up expired OTP for email: {Email}", email);
        }
    }

    /// <summary>
    /// Internal class to store OTP data.
    /// In production, this would be stored in Redis or database.
    /// </summary>
    private class OtpData
    {
        public string Code { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int Attempts { get; set; }
        public bool IsUsed { get; set; }
    }
}
