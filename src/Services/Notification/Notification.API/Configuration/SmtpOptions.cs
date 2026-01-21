namespace Notification.API.Configuration;

public class SmtpOptions
{
    public const string SectionName = "Smtp";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SenderName { get; set; } = "Marketplace Support";
    public string SenderEmail { get; set; } = "no-reply@market.com";
}
