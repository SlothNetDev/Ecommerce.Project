namespace Ecommerce.Core.Application.Settings;

/// <summary>
/// Configuration settings for email service providers.
/// Used to configure SMTP settings, API keys, and other email-related configuration.
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// Email provider name (e.g. "Resend")
    /// </summary>
    public string Provider { get; init; } = "Resend";

    /// <summary>
    /// Default sender email address (must be verified in Resend)
    /// </summary>
    public string FromEmail { get; init; } = string.Empty;

    /// <summary>
    /// Friendly sender display name
    /// </summary>
    public string FromName { get; init; } = "Ecommerce App";

    /// <summary>
    /// Resend API Key (store in User Secrets / Key Vault)
    /// </summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>
    /// Public base URL used in email links
    /// </summary>
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// OTP expiration window in minutes
    /// </summary>
    public int OtpExpiryMinutes { get; init; } = 10;
}