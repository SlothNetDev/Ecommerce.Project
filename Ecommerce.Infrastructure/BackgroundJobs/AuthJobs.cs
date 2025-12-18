using System.Linq.Expressions;
using Ecommerce.Core.Application.Common.Interfaces.Notification;
using Ecommerce.Core.Application.Common.Interfaces.Register;
using Hangfire;

namespace Ecommerce.Infrastructure.BackgroundJobs;

public class AuthJobs(IEmailService emailService)
{
    public async Task SendOtpJob(string email, string code, string name)
    {
        await emailService.SendOtpEmailAsync(email, code, name);
    }
}

public interface IBackgroundJobService
{
    void Enqueue(Expression<Action> job);
}
public class HangfireBackgroundJobService : IBackgroundJobService
{
    public void Enqueue(Expression<Action> job)
        => BackgroundJob.Enqueue(job);
}
public class FakeBackgroundJobService : IBackgroundJobService
{
    public List<string> EnqueuedJobs { get; } = new();

    public void Enqueue(Expression<Action> job)
    {
        EnqueuedJobs.Add(job.Body.ToString());
    }
}
