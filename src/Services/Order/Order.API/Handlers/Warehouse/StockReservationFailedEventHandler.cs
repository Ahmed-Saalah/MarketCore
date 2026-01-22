using Core.Messaging;
using Microsoft.EntityFrameworkCore;
using Order.API.Constants;
using Order.API.Data;

namespace Order.API.Handlers.Warehouse;

public sealed class StockReservationFailedEventHandler
{
    public sealed class Handler(OrderDbContext dbContext, ILogger<Handler> logger)
        : IEventHandler<Event>
    {
        public async Task HandleAsync(Event @event, CancellationToken cancellationToken = default)
        {
            var order = await dbContext.Orders.FindAsync(@event.OrderId);
            if (order == null)
            {
                return;
            }

            order.Status = OrderStatus.Cancelled;
            order.CancelledAt = DateTime.UtcNow;
            logger.LogWarning("Order {OrderId} failed due to: {Reason}", order.Id, @event.Reason);

            await dbContext.SaveChangesAsync();
        }
    }

    public sealed record Event(Guid OrderId, Guid StoreId, string Reason);
}
