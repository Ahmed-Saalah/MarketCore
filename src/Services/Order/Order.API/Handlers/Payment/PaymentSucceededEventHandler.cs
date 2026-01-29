using Core.Messaging;
using Microsoft.EntityFrameworkCore;
using Order.API.Constants;
using Order.API.Data;
using Order.API.Messages;

namespace Order.API.Handlers.Payment;

public sealed class PaymentSucceededEventHandler
{
    public sealed class Handler(
        OrderDbContext dbContext,
        IEventPublisher eventPublisher,
        ILogger<Handler> logger
    ) : IEventHandler<Event>
    {
        public async Task HandleAsync(Event @event, CancellationToken cancellationToken)
        {
            logger.LogInformation(
                "Payment Succeeded for Order {OrderId}. Finalizing...",
                @event.OrderId
            );

            var order = await dbContext
                .Orders.Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == @event.OrderId, cancellationToken);

            if (order == null)
            {
                logger.LogError(
                    "Order {OrderId} not found while processing Payment Success.",
                    @event.OrderId
                );
                return;
            }

            order.Status = OrderStatus.Paid;
            order.CompletedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Order {OrderId} is now PAID and COMPLETED.", @event.OrderId);

            await eventPublisher.PublishAsync(
                new OrderCompletedEvent(
                    @event.OrderId,
                    order.UserId,
                    order.StoreId,
                    order.Total,
                    order
                        .Items.Select(i => new OrderCompletedItemDto(i.ProductId, i.Quantity))
                        .ToList()
                ),
                cancellationToken
            );
        }
    }

    [MessageKey("Payment.PaymentSucceededEvent")]
    public sealed record Event(
        Guid OrderId,
        Guid PaymentId,
        decimal Amount,
        string PaymentIntentId
    );
}
