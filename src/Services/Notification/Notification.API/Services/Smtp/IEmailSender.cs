namespace Notification.API.Services.Smtp;

public interface IEmailSender
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string body);
}
