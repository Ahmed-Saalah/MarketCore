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
    ) : IRequest;

    public sealed class Handler(WarehouseDbContext dbContext) : IRequestHandler<Event>
    {
        public async Task Handle(Event @event, CancellationToken ct)
        {
            var inventory = await dbContext.Inventory.FirstOrDefaultAsync(
                i => i.ProductId == @event.Id,
                ct
            );

            if (inventory is null)
            {
                return;
            }

            if (inventory.Sku != @event.Sku)
            {
                inventory.Sku = @event.Sku;
                await dbContext.SaveChangesAsync(ct);
            }
        }
    }
}
