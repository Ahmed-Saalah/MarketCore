using System.Text;
using Core.Messaging;
using Notification.API.Clients.Customer.Interfaces;
using Notification.API.Data;
using Notification.API.Services.Smtp;

namespace Notification.API.Handlers.Order;

public sealed class OrderCreatedEventHandler
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
                "Processing Cancellation Email for Order {OrderId}",
                @event.OrderId
            );

            var customer = await customerClient.GetCustomerAsync(@event.UserId, cancellationToken);
            var bodyBuilder = new StringBuilder();
            bodyBuilder.AppendLine($"<h1>Hi {customer.DisplayName},</h1>");
            bodyBuilder.AppendLine(
                $"<p>Your order <strong>#{@event.OrderId}</strong> has been created.</p>"
            );
            bodyBuilder.AppendLine("<hr/>");
            string subject = $"Order Created: #{@event.OrderId.ToString().Substring(0, 8)}";
            string body = bodyBuilder.ToString();

            bool isSuccess = await emailSender.SendEmailAsync(customer.Email, subject, body);

            var log = new Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = @event.UserId,
                EventType = "OrderCreated",
                RecipientEmail = customer.Email,
                Subject = subject,
                IsSuccess = isSuccess,
                ErrorMessage = isSuccess ? null : "SMTP Delivery Failed",
                SentAt = DateTime.UtcNow,
            };

            dbContext.Notifications.Add(log);
            await dbContext.SaveChangesAsync(cancellationToken);
            if (!isSuccess)
            {
                logger.LogError(
                    "Failed to send cancellation email for Order {OrderId}",
                    @event.OrderId
                );
            }
        }
    }

    public record Event(Guid OrderId, Guid StoreId, Guid UserId, DateTime CreatedAt);
}
