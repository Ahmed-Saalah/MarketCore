using Core.Messaging;
using Microsoft.EntityFrameworkCore;
using Warehouse.API.Data;
using Warehouse.API.Messages;

namespace Warehouse.API.Handlers.Orders.Commands;

public sealed class ReserveStockCommandHandler
{
    public sealed record Command(Guid OrderId, Guid StoreId, List<OrderItemDto> Items);

    public sealed record OrderItemDto(Guid ProductId, int Quantity);

    public sealed class Handler(
        WarehouseDbContext dbContext,
        IEventPublisher eventPublisher,
        ILogger<Handler> logger
    ) : IEventHandler<Command>
    {
        public async Task HandleAsync(
            Command command,
            CancellationToken cancellationToken = default
        )
        {
            using var transaction = await dbContext.Database.BeginTransactionAsync(
                cancellationToken
            );
            try
            {
                foreach (var item in command.Items)
                {
                    // RESUME HIGHLIGHT: Optimistic Concurrency Control Logic
                    // We assume we can update, but we check the version/quantity at the very end.
                    var inventory = await dbContext.Inventory.FirstOrDefaultAsync(
                        i => i.ProductId == item.ProductId && i.StoreId == command.StoreId,
                        cancellationToken
                    );

                    if (inventory == null)
                    {
                        throw new Exception($"Product {item.ProductId} not found in store.");
                    }

                    if (inventory.QuantityOnHand - inventory.ReservedQuantity < item.Quantity)
                    {
                        throw new Exception($"Insufficient stock for Product {item.ProductId}.");
                    }

                    inventory.ReservedQuantity += item.Quantity;
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                await eventPublisher.PublishAsync(
                    new StockReservedEvent(command.OrderId, command.StoreId),
                    "Warehouse.StockReservedEvent",
                    cancellationToken
                );
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // TODO: Implement retry logic
                logger.LogWarning(
                    ex,
                    "Concurrency conflict for Order {OrderId}. Retrying...",
                    command.OrderId
                );
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                await eventPublisher.PublishAsync(
                    new StockReservationFailedEvent(
                        command.OrderId,
                        command.StoreId,
                        ex.Message,
                        command.Items.Select(i => new Item(i.ProductId, i.Quantity)).ToList()
                    ),
                    "Warehouse.StockReservationFailedEvent",
                    cancellationToken
                );
            }
        }
    }
}
