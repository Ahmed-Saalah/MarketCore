using Core.Messaging;
using Elastic.Clients.Elasticsearch;
using MediatR;
using Search.API.Models;

namespace Search.API.Handlers.Catalog;

public sealed class ProductActivatedEventHandler
{
    public sealed record Event(Guid ProductId, DateTime Timestamp);

    public sealed class Handler(ElasticsearchClient client) : IEventHandler<Event>
    {
        public async Task HandleAsync(Event @event, CancellationToken cancellationToken)
        {
            await client.UpdateAsync<Product, object>(
                "products",
                @event.ProductId,
                u => u.Doc(new { IsActive = true }),
                cancellationToken
            );
        }
    }
}
