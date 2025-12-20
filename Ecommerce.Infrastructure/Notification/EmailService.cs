using Ecommerce.Core.Application.Common.Interfaces.Notification;
using Ecommerce.Core.Application.Common.Interfaces.Register;
using Ecommerce.Core.Application.Settings;
using FluentEmail.Core;
using FluentEmail.SendGrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

namespace Ecommerce.Infrastructure.Notification;

/// <summary>
/// Service for sending emails via SendGrid.
/// Uses FluentEmail for clean, testable email composition and sending.
/// </summary>
public class EmailService(IResend resend,
    ILogger<EmailService> logger,
    IOptions<EmailSettings> settings)
    : IEmailService
{
    /// <inheritdoc/>
    public async Task<bool> SendOtpEmailAsync(string email, string otpCode, string? userName = null)
    {
        var subject = "Your Verification Code";

        var body = $"""
                        <h2>Hello {userName ?? "there"},</h2>
                        <p>Your one-time verification code is:</p>
                        <h1 style="letter-spacing: 3px;">{otpCode}</h1>
                        <p>This code expires in {settings.Value.OtpExpiryMinutes} minutes.</p>
                    """;

        return await SendAsync(email, subject, body);
    }

    /// <inheritdoc/>
    public async Task<bool> SendWelcomeEmailAsync(string email, string userName, string firstName)
    {
        var subject = $"Welcome aboard, {firstName}!";

        var body = $"""
                        <h2>Welcome, {userName} 🎉</h2>
                        <p>Thanks for joining our community.</p>
                        <p>You can now log in and start exploring.</p>
                    """;

        return await SendAsync(email, subject, body);
    }

    /// <inheritdoc/>
    public async Task<bool> SendPasswordResetEmailAsync(string email, string resetToken, string userName)
    {
        var resetLink = $"{settings.Value.BaseUrl}/reset-password?token={resetToken}";

        var subject = "Reset Your Password";

        var body = $"""
                        <p>Hello {userName},</p>
                        <p>You requested to reset your password.</p>
                        <p>
                            <a href="{resetLink}">
                                Click here to reset your password
                            </a>
                        </p>
                        <p>If you did not request this, you can safely ignore this email.</p>
                    """;

        return await SendAsync(email, subject, body);
    }


    private async Task<bool> SendAsync(
        string to,
        string subject,
        string htmlBody)
    {
        try
        {
            var message = new EmailMessage
            {
                From = $"{settings.Value.FromName} <{settings.Value.FromEmail}>",
                Subject = subject,
                HtmlBody = htmlBody
            };

            message.To.Add(to);

            var response = await resend.EmailSendAsync(message);

            if (response is null)
            {
                logger.LogWarning("Email sending failed: null response (To={Email})", to);
                return false;
            }

            logger.LogInformation(
                "Email sent successfully (To={Email}, MessageId={MessageId})",
                to,
                response);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email (To={Email})", to);
            return false;
        }
    }
}
