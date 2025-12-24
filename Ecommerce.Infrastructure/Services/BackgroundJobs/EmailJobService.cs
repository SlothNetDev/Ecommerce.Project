using Ecommerce.Core.Application.Common.Interfaces.BackGroundJob;
using Ecommerce.Core.Application.Common.Interfaces.Notification;

namespace Ecommerce.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Will be used by Hangfire to send emails. By this we can safely swap Email Providers
/// </summary>
/// <param name="emailService"></param>
public class EmailJobService(IEmailService emailService) : IEmailJobService
{
    public Task SendOtpAsync(string email, string otp, string? name)
        => emailService.SendOtpEmailAsync(email, otp, name);

    public Task SendWelcomeAsync(string email, string userName, string firstName)
     => emailService.SendWelcomeEmailAsync(email, userName, firstName); 

    public Task SendPasswordResetAsync(string email, string resetToken, string userName)
    => emailService.SendPasswordResetEmailAsync(email, resetToken, userName);
}