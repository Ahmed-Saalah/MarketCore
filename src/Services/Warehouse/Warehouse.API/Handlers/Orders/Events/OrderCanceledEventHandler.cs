using Core.Messaging;
using Microsoft.EntityFrameworkCore;
using Warehouse.API.Data;

namespace Warehouse.API.Handlers.Orders.Events;

public sealed class OrderCanceledEventHandler
{
    public sealed class Handler(WarehouseDbContext dbContext) : IEventHandler<Event>
    {
        public async Task HandleAsync(Event @event, CancellationToken cancellationToken = default)
        {
            foreach (var item in @event.Items)
            {
                var inventory = await dbContext.Inventory.FirstOrDefaultAsync(
                    i => i.ProductId == item.ProductId,
                    cancellationToken: cancellationToken
                );

                if (inventory != null)
                {
                    inventory.ReservedQuantity -= item.Quantity;
                    if (inventory.ReservedQuantity < 0)
                        inventory.ReservedQuantity = 0;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    [MessageKey("Order.OrderCanceledEvent")]
    public sealed record Event(
        Guid OrderId,
        Guid UserId,
        Guid StoreId,
        decimal Total,
        string Reason,
        List<OrderCanceledItemDto> Items
    );

    public sealed record OrderCanceledItemDto(Guid ProductId, int Quantity);
}
