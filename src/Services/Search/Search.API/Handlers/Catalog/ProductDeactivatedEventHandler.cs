using Core.Messaging;
using Elastic.Clients.Elasticsearch;
using Search.API.Models;

namespace Search.API.Handlers.Catalog;

public sealed class ProductDeactivatedEventHandler
{
    public sealed class Handler(ElasticsearchClient client) : IEventHandler<Event>
    {
        public async Task HandleAsync(Event @event, CancellationToken cancellationToken)
        {
            await client.UpdateAsync<Product, object>(
                "products",
                @event.ProductId,
                u => u.Doc(new { IsActive = false }),
                cancellationToken
            );
        }
    }

    [MessageKey("Catalog.ProductDeactivatedEvent")]
    public sealed record Event(Guid ProductId, DateTime Timestamp);
}
