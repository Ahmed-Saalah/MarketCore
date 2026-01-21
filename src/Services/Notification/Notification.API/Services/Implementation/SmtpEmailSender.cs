using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using Notification.API.Configuration;
using Notification.API.Services.Interfaces;

namespace Notification.API.Services.Implementation;

public class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    : IEmailSender
{
    private readonly SmtpOptions _options = options.Value;
    private readonly ILogger<SmtpEmailSender> _logger = logger;

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(_options.SenderName, _options.SenderEmail));

            message.To.Add(MailboxAddress.Parse(toEmail));

            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();

            await client.ConnectAsync(
                _options.Host,
                _options.Port,
                MailKit.Security.SecureSocketOptions.StartTls
            );

            if (!string.IsNullOrEmpty(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password);
            }

            await client.SendAsync(message);

            await client.DisconnectAsync(true);
            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            return false;
        }
    }
}
