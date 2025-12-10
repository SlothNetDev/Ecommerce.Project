using Ecommerce.Core.Application.Common.Interfaces.Notification;
using Ecommerce.Core.Application.Common.Interfaces.Register;
using FluentEmail.Core;
using FluentEmail.SendGrid;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Notification;

/// <summary>
/// Service for sending emails via SendGrid.
/// Uses FluentEmail for clean, testable email composition and sending.
/// </summary>
public class EmailService(IFluentEmail fluentEmail, ILogger<EmailService> logger)
    : IEmailService
{
    /// <inheritdoc/>
    public async Task<bool> SendOtpEmailAsync(string email, string otpCode, string? userName = null)
    {
        try
        {
            var greeting = string.IsNullOrEmpty(userName) ? "Hello" : $"Hello {userName}";
            var body = GenerateOtpEmailBody(greeting, otpCode);

            var emailResponse = await fluentEmail
                .To(email)
                .Subject("Your Verification Code")
                .Body(body, isHtml: true)
                .SendAsync();

            if (emailResponse.Successful)
            {
                logger.LogInformation("OTP email sent successfully to: {Email}", email);
                return true;
            }
            else
            {
                logger.LogError("Failed to send OTP email to: {Email}. Errors: {Errors}",
                    email, string.Join(", ", emailResponse.ErrorMessages));
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send OTP email to: {Email}", email);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SendWelcomeEmailAsync(string email, string userName, string firstName)
    {
        try
        {
            var body = GenerateWelcomeEmailBody(firstName);

            var emailResponse = await fluentEmail
                .To(email)
                .Subject("Welcome to Our Platform!")
                .Body(body, isHtml: true)
                .SendAsync();

            if (emailResponse.Successful)
            {
                logger.LogInformation("Welcome email sent successfully to: {Email}", email);
                return true;
            }
            else
            {
                logger.LogError("Failed to send welcome email to: {Email}. Errors: {Errors}",
                    email, string.Join(", ", emailResponse.ErrorMessages));
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send welcome email to: {Email}", email);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SendPasswordResetEmailAsync(string email, string resetToken, string userName)
    {
        try
        {
            var body = GeneratePasswordResetEmailBody(userName, resetToken);

            var emailResponse = await fluentEmail
                .To(email)
                .Subject("Password Reset Request")
                .Body(body, isHtml: true)
                .SendAsync();

            if (emailResponse.Successful)
            {
                logger.LogInformation("Password reset email sent successfully to: {Email}", email);
                return true;
            }
            else
            {
                logger.LogError("Failed to send password reset email to: {Email}. Errors: {Errors}",
                    email, string.Join(", ", emailResponse.ErrorMessages));
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send password reset email to: {Email}", email);
            return false;
        }
    }


    /// <summary>
    /// Generates the HTML body for OTP verification emails.
    /// </summary>
    private static string GenerateOtpEmailBody(string greeting, string otpCode)
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Email Verification</title>
            <style>
                body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f4f4f4; }}
                .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; }}
                .header {{ background-color: #007bff; color: #ffffff; padding: 20px; text-align: center; }}
                .content {{ padding: 30px; }}
                .otp-code {{ font-size: 32px; font-weight: bold; color: #007bff; text-align: center; margin: 20px 0; padding: 15px; background-color: #f8f9fa; border: 2px dashed #007bff; border-radius: 5px; }}
                .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; color: #666666; font-size: 12px; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1>Email Verification</h1>
                </div>
                <div class='content'>
                    <p>{greeting},</p>
                    <p>Thank you for registering with our platform. To complete your registration, please use the verification code below:</p>
        
                    <div class='otp-code'>{otpCode}</div>
        
                    <p><strong>Important:</strong></p>
                    <ul>
                        <li>This code will expire in 10 minutes</li>
                        <li>Do not share this code with anyone</li>
                        <li>If you didn't request this, please ignore this email</li>
                    </ul>
        
                    <p>If you have any questions, please contact our support team.</p>
                </div>
                <div class='footer'>
                    <p>This is an automated message. Please do not reply to this email.</p>
                    <p>&copy; 2024 Your App Name. All rights reserved.</p>
                </div>
            </div>
        </body>
        </html>";
            }
        
            /// <summary>
            /// Generates the HTML body for welcome emails.
            /// </summary>
            private static string GenerateWelcomeEmailBody(string firstName)
            {
                return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Welcome!</title>
            <style>
                body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f4f4f4; }}
                .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; }}
                .header {{ background-color: #28a745; color: #ffffff; padding: 20px; text-align: center; }}
                .content {{ padding: 30px; }}
                .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; color: #666666; font-size: 12px; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1>Welcome {firstName}!</h1>
                </div>
                <div class='content'>
                    <p>Congratulations! Your account has been successfully verified and activated.</p>
                    <p>You can now:</p>
                    <ul>
                        <li>Log in to your account</li>
                        <li>Access all platform features</li>
                        <li>Update your profile information</li>
                    </ul>
                    <p>We're excited to have you as part of our community!</p>
                </div>
                <div class='footer'>
                    <p>&copy; 2024 Your App Name. All rights reserved.</p>
                </div>
            </div>
        </body>
        </html>";
            }
        
            /// <summary>
            /// Generates the HTML body for password reset emails.
            /// </summary>
            private static string GeneratePasswordResetEmailBody(string userName, string resetToken)
            {
                return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Password Reset</title>
            <style>
                body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f4f4f4; }}
                .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; }}
                .header {{ background-color: #dc3545; color: #ffffff; padding: 20px; text-align: center; }}
                .content {{ padding: 30px; }}
                .reset-code {{ font-size: 24px; font-weight: bold; color: #dc3545; text-align: center; margin: 20px 0; padding: 15px; background-color: #f8f9fa; border: 2px dashed #dc3545; border-radius: 5px; }}
                .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; color: #666666; font-size: 12px; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1>Password Reset Request</h1>
                </div>
                <div class='content'>
                    <p>Hello {userName},</p>
                    <p>We received a request to reset your password. Use the code below to reset your password:</p>
        
                    <div class='reset-code'>{resetToken}</div>
        
                    <p><strong>Security Notice:</strong></p>
                    <ul>
                        <li>This code will expire in 15 minutes</li>
                        <li>If you didn't request this reset, please ignore this email</li>
                        <li>Your password will remain unchanged until you use this code</li>
                    </ul>
        
                    <p>If you have any questions, please contact our support team.</p>
                </div>
                <div class='footer'>
                    <p>This is an automated message. Please do not reply to this email.</p>
                    <p>&copy; 2024 Your App Name. All rights reserved.</p>
                </div>
            </div>
        </body>
        </html>";
            }
}
