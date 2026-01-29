using Core.Messaging;
using Elastic.Clients.Elasticsearch;
using Search.API.Constants;
using Search.API.Models;

namespace Search.API.Handlers.Warehouse;

public sealed class ProductBackInStockEventHandler
{
    public sealed class Handler(ElasticsearchClient client) : IEventHandler<Event>
    {
        public async Task HandleAsync(Event @event, CancellationToken cancellationToken = default)
        {
            await client.UpdateAsync<Product, object>(
                "products",
                @event.ProductId,
                u => u.Doc(new { StockStatus = StockStatus.Available }),
                cancellationToken
            );
        }
    }

    [MessageKey("Warehouse.ProductBackInStockEvent")]
    public sealed record Event(Guid ProductId, Guid StoreId, Guid InventoryId, DateTime Timestamp);
}
