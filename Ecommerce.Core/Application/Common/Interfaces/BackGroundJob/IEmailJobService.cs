namespace Ecommerce.Core.Application.Common.Interfaces.BackGroundJob;

public interface IEmailJobService
{
    Task SendOtpAsync(string email, string otp, string? name);
    Task SendWelcomeAsync(string email, string userName, string firstName);
    Task SendPasswordResetAsync(string email, string resetToken, string userName);
}