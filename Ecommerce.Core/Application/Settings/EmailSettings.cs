namespace Ecommerce.Core.Application.Settings;

/// <summary>
/// Configuration settings for email service providers.
/// Used to configure SMTP settings, API keys, and other email-related configuration.
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// The email service provider to use (e.g., "Smtp", "SendGrid", "Mailgun").
    /// </summary>
    public string Provider { get; set; } = "Smtp";

    /// <summary>
    /// SMTP server hostname or IP address.
    /// </summary>
    public string SmtpServer { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port (typically 587 for TLS, 465 for SSL, 25 for plain).
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// Username for SMTP authentication.
    /// </summary>
    public string SmtpUsername { get; set; } = string.Empty;

    /// <summary>
    /// Password for SMTP authentication.
    /// </summary>
    public string SmtpPassword { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use SSL/TLS encryption.
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// The email address to use as the sender.
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// The display name for the sender.
    /// </summary>
    public string FromName { get; set; } = "Your App";

    /// <summary>
    /// API key for cloud email providers (SendGrid, Mailgun, etc.).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for the application (used for generating links in emails).
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
}