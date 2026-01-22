using System.Text;
using Core.Messaging;
using Notification.API.Clients.Customer.Interfaces;
using Notification.API.Data;
using Notification.API.Services.Smtp;

namespace Notification.API.Handlers.Order;

public sealed class OrderCompletedEventHandler
{
    public sealed class Handler(
        IEmailSender emailSender,
        NotificationDbContext dbContext,
        ICustomerApiClient customerClient,
        ILogger<Handler> logger
    ) : IEventHandler<Event>
    {
        public async Task HandleAsync(Event @event, CancellationToken cancellationToken = default)
        {
            logger.LogInformation(
                "Preparing Order Confirmation Email for Order {OrderId}",
                @event.OrderId
            );

            var customer = await customerClient.GetCustomerAsync(@event.UserId, cancellationToken);

            var bodyBuilder = new StringBuilder();
            bodyBuilder.AppendLine($"<h1>Hi {customer.DisplayName},</h1>");
            bodyBuilder.AppendLine($"<h1>Order Confirmed!</h1>");
            bodyBuilder.AppendLine($"<p>Order ID: <strong>{@event.OrderId}</strong></p>");
            bodyBuilder.AppendLine("<hr/>");
            bodyBuilder.AppendLine("<h3>Items:</h3><ul>");
            bodyBuilder.AppendLine("</ul>");
            bodyBuilder.AppendLine($"<h3>Total Paid: {@event.Total:C}</h3>");
            bodyBuilder.AppendLine("<p>Thank you for shopping with us!</p>");

            string subject = $"Order Confirmation #{@event.OrderId.ToString().Substring(0, 8)}";
            string body = bodyBuilder.ToString();

            bool isSuccess = await emailSender.SendEmailAsync(customer.Email, subject, body);

            var log = new Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = @event.UserId,
                EventType = "OrderCompleted",
                RecipientEmail = customer.Email,
                Subject = subject,
                BodyPreview = $"Total: {@event.Total:C}",
                IsSuccess = isSuccess,
                ErrorMessage = isSuccess ? null : "SMTP Delivery Failed",
                SentAt = DateTime.UtcNow,
            };

            dbContext.Notifications.Add(log);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (!isSuccess)
            {
                logger.LogError("Failed to send email for Order {OrderId}", @event.OrderId);
            }
        }
    }

    public sealed record Event(Guid OrderId, Guid UserId, Guid StoreId, decimal Total);
}
