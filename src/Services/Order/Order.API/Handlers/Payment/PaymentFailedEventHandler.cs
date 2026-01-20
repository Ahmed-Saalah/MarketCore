using Core.Messaging;
using Order.API.Constants;
using Order.API.Data;
using Order.API.Messages;

namespace Order.API.Handlers.Payment;

public sealed class PaymentFailedEventHandler
{
    public sealed record Event(Guid OrderId, string Reason);

    public sealed class Handler(
        OrderDbContext dbContext,
        IEventPublisher eventPublisher,
        ILogger<Handler> logger
    ) : IEventHandler<Event>
    {
        public async Task HandleAsync(Event @event, CancellationToken cancellationToken = default)
        {
            logger.LogWarning(
                "Payment Failed for Order {OrderId}. Reason: {Reason}",
                @event.OrderId,
                @event.Reason
            );

            var order = await dbContext.Orders.FindAsync([@event.OrderId], cancellationToken);

            if (order == null)
            {
                logger.LogError(
                    "Order {OrderId} not found while processing Payment Failure.",
                    @event.OrderId
                );
                return;
            }

            order.Status = OrderStatus.Cancelled;
            order.CancelledAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Order {OrderId} has been CANCELLED due to payment failure.",
                @event.OrderId
            );

            await eventPublisher.PublishAsync(
                new OrderCanceledEvent(
                    @event.OrderId,
                    order.UserId,
                    order.StoreId,
                    order.Total,
                    @event.Reason
                ),
                "Order.OrderCanceledEvent",
                cancellationToken
            );
        }
    }
}
