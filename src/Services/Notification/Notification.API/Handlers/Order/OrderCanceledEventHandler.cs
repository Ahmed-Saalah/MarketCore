using System.Text;
using Core.Messaging;
using Notification.API.Clients.Customer.Interfaces;
using Notification.API.Data;
using Notification.API.Services.Interfaces;

namespace Notification.API.Handlers.Order;

public sealed class OrderCanceledEventHandler
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
                $"<p>Your order <strong>#{@event.OrderId}</strong> has been canceled.</p>"
            );

            bodyBuilder.AppendLine($"<p>Reason: <strong>{@event.Reason}</strong></p>");

            bodyBuilder.AppendLine(
                "<p>If you have already been charged, a refund will be processed within 3-5 business days.</p>"
            );
            bodyBuilder.AppendLine("<hr/>");
            bodyBuilder.AppendLine("<p>We apologize for the inconvenience.</p>");

            string subject = $"Order Canceled: #{@event.OrderId.ToString().Substring(0, 8)}";
            string body = bodyBuilder.ToString();

            bool isSuccess = await emailSender.SendEmailAsync(customer.Email, subject, body);

            var log = new Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = @event.UserId,
                EventType = "OrderCanceled",
                RecipientEmail = customer.Email,
                Subject = subject,
                BodyPreview = $"Reason: {@event.Reason}",
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

    public sealed record Event(
        Guid OrderId,
        Guid UserId,
        Guid StoreId,
        decimal Total,
        string Reason
    );
}
