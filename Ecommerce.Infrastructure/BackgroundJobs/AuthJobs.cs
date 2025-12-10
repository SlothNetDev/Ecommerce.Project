using Ecommerce.Core.Application.Common.Interfaces.Notification;
using Ecommerce.Core.Application.Common.Interfaces.Register;

namespace Ecommerce.Infrastructure.BackgroundJobs;

public class AuthJobs(IEmailService emailService)
{
    public async Task SendOtpJob(string email, string code, string name)
    {
        await emailService.SendOtpEmailAsync(email, code, name);
    }
}