using Ecommerce.Core.Application.Common.Interfaces.Notification;
using Ecommerce.Core.Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

namespace Ecommerce.Infrastructure.DevelopmentService.Notification;

/// <summary>
/// Service for sending emails via SendGrid.
/// Uses FluentEmail for clean, testable email composition and sending.
/// </summary>
public class DevEmailService(DevEmailStore store,
    ILogger<DevEmailService> logger)
    : IEmailService
{
    /// <inheritdoc/>
    public Task<bool> SendOtpEmailAsync(string email, string otpCode, string? userName = null)
    {
        store.Add(new DevEmailMessage(
            email,
            "DEV: Your OTP Code",
            $"Hello {userName}, your OTP is {otpCode}",
            DateTime.UtcNow
        ));
        
        // Debug: Check if the store actually has items right after adding
        logger.LogInformation("Store now contains {Count} emails", store.GetAllEmails().Count);
        logger.LogWarning("[DEV EMAIL] OTP sent to {Email}: {Otp}", email, otpCode);
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task<bool> SendWelcomeEmailAsync(string email, string userName, string firstName)
    {
        store.Add(new DevEmailMessage(
            email,
            "DEV: Welcome",
            $"Welcome {firstName}",
            DateTime.UtcNow
        ));

        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task<bool> SendPasswordResetEmailAsync(string email, string resetToken, string userName)
    {
        store.Add(new DevEmailMessage(
            email,
            "DEV: Password Reset",
            $"Reset token: {resetToken}",
            DateTime.UtcNow
        ));

        return Task.FromResult(true);
    }
    
}
