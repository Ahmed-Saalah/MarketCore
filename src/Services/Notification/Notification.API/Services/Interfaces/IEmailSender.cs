namespace Notification.API.Services.Interfaces;

public interface IEmailSender
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string body);
}
