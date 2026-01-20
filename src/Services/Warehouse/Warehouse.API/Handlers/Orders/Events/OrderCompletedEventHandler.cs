using Core.Messaging;
using Microsoft.EntityFrameworkCore;
using Warehouse.API.Data;
using Warehouse.API.Entities;

namespace Warehouse.API.Handlers.Orders.Events;

public sealed class OrderCompletedEventHandler
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
                    inventory.QuantityOnHand -= item.Quantity;

                    var transaction = new StockTransaction
                    {
                        Id = Guid.NewGuid(),
                        InventoryId = inventory.Id,
                        StoreId = inventory.StoreId,
                        Type = TransactionType.Sale,
                        QuantityChanged = -item.Quantity,
                        ReferenceId = @event.OrderId.ToString(),
                        CreatedAt = DateTime.UtcNow,
                    };

                    dbContext.StockTransactions.Add(transaction);
                }
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public sealed record Event(
        Guid OrderId,
        Guid UserId,
        Guid StoreId,
        decimal Total,
        List<OrderCompletedItemDto> Items
    );

    public sealed record OrderCompletedItemDto(Guid ProductId, int Quantity);
}
