using Core.Messaging;
using Microsoft.EntityFrameworkCore;
using Warehouse.API.Data;
using Warehouse.API.Entities;
using Warehouse.API.Messages;

namespace Warehouse.API.Handlers.Orders.Events;

public sealed class OrderCompletedEventHandler
{
    public sealed class Handler(WarehouseDbContext dbContext, IEventPublisher eventPublisher)
        : IEventHandler<Event>
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
                    int previousQuantity = inventory.QuantityOnHand;
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

                    if (previousQuantity > 0 && inventory.QuantityOnHand <= 0)
                    {
                        await eventPublisher.PublishAsync(
                            new ProductOutOfStockEvent(
                                item.ProductId,
                                @event.StoreId,
                                inventory.Id,
                                DateTime.UtcNow
                            ),
                            cancellationToken
                        );
                    }
                    else if (
                        previousQuantity > 5
                        && inventory.QuantityOnHand <= 5
                        && inventory.QuantityOnHand > 0
                    )
                    {
                        await eventPublisher.PublishAsync(
                            new ProductLowStockEvent(
                                item.ProductId,
                                @event.StoreId,
                                inventory.Id,
                                DateTime.UtcNow
                            ),
                            cancellationToken
                        );
                    }

                    dbContext.StockTransactions.Add(transaction);
                }
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    [MessageKey("Order.OrderCompletedEvent")]
    public sealed record Event(
        Guid OrderId,
        Guid UserId,
        Guid StoreId,
        decimal Total,
        List<OrderCompletedItemDto> Items
    );

    public sealed record OrderCompletedItemDto(Guid ProductId, int Quantity);
}
