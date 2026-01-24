using Core.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Warehouse.API.Data;
using Warehouse.API.Entities;

namespace Warehouse.API.Handlers.Products;

public sealed class ProductCreatedEventHandler
{
    public sealed record Event(
        Guid ProductId,
        Guid StoreId,
        string Sku,
        string Name,
        string Description,
        decimal Price,
        Guid CategoryId,
        string? CategoryName,
        DateTime CreatedAt
    );

    public sealed class Handler(WarehouseDbContext dbContext) : IEventHandler<Event>
    {
        public async Task HandleAsync(Event @event, CancellationToken cancellationToken = default)
        {
            var exists = await dbContext.Inventory.AnyAsync(
                i => i.ProductId == @event.ProductId && i.StoreId == @event.StoreId,
                cancellationToken
            );

            if (exists)
            {
                return;
            }

            var newInventory = new Inventory
            {
                Id = Guid.NewGuid(),
                StoreId = @event.StoreId,
                ProductId = @event.ProductId,
                Sku = @event.Sku,
                QuantityOnHand = 0,
                ReservedQuantity = 0,
            };

            dbContext.Inventory.Add(newInventory);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
