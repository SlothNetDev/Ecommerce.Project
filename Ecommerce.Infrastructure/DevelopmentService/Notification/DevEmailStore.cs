namespace Ecommerce.Infrastructure.DevelopmentService.Notification;

public class DevEmailStore
{
    private readonly List<DevEmailMessage> _devEmailMessages = new();

    public void Add(DevEmailMessage emailMessage) => _devEmailMessages.Add(emailMessage);
    
    public List<DevEmailMessage> GetAllEmails()
    {
        return _devEmailMessages.OrderByDescending(x => x.SentAt).ToList();
    }
}


public record DevEmailMessage(
    string To,
    string Subject,
    string Body,
    DateTime SentAt
);