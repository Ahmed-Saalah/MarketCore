using Core.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Warehouse.API.Data;

namespace Warehouse.API.Handlers;

public sealed class ProductUpdatedEventHandler
{
    public sealed record Event(
        Guid Id,
        string Name,
        string Description,
        decimal Price,
        string Sku,
        Guid StoreId
    );

    public sealed class Handler(WarehouseDbContext dbContext) : IEventHandler<Event>
    {
        public async Task HandleAsync(Event @event, CancellationToken cancellationToken = default)
        {
            var inventory = await dbContext.Inventory.FirstOrDefaultAsync(
                i => i.ProductId == @event.Id,
                cancellationToken
            );

            if (inventory is null)
            {
                return;
            }

            if (inventory.Sku != @event.Sku)
            {
                inventory.Sku = @event.Sku;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
