using Core.Messaging;
using Elastic.Clients.Elasticsearch;
using Search.API.Models;

namespace Search.API.Handlers.Warehouse;

public sealed class ProductBackInStockEventHandler
{
    public sealed class Handler(ElasticsearchClient client) : IEventHandler<Event>
    {
        public async Task HandleAsync(Event @event, CancellationToken cancellationToken = default)
        {
            await client.UpdateAsync<Product, object>(
                new UpdateRequest<Product, object>(index: "products", id: new Id(@event.ProductId))
                {
                    Doc = new { StockStatus = "Available" },
                },
                cancellationToken
            );
        }
    }

    public sealed record Event(Guid ProductId, Guid StoreId, Guid InventoryId, DateTime Timestamp);
}
