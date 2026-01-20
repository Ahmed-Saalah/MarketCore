using Core.Messaging;
using Order.API.Constants;
using Order.API.Data;
using Order.API.Messages;

namespace Order.API.Handlers.Warehouse;

public sealed class StockReservedEventHandler
{
    public sealed record Event(Guid OrderId, Guid StoreId);

    public sealed class Handler(
        OrderDbContext dbContext,
        IEventPublisher eventPublisher,
        ILogger<Handler> logger
    ) : IEventHandler<Event>
    {
        public async Task HandleAsync(Event @event, CancellationToken cancellationToken = default)
        {
            var order = await dbContext.Orders.FindAsync(@event.OrderId);
            if (order == null)
            {
                return;
            }

            order.Status = OrderStatus.PendingPayment;
            await dbContext.SaveChangesAsync();

            logger.LogInformation(
                "Stock reserved for Order {OrderId}. Proceeding to payment...",
                order.Id
            );

            await eventPublisher.PublishAsync(
                new CreatePaymentCommand(order.Id, order.UserId, order.Total, "USD"),
                "Order.CreatePaymentCommand",
                cancellationToken
            );
        }
    }
}
