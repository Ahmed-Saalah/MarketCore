using Core.Messaging;
using Elastic.Clients.Elasticsearch;
using MediatR;
using Search.API.Models;

namespace Search.API.Handlers.Catalog;

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

    public sealed class Handler(ElasticsearchClient client) : IEventHandler<Event>
    {
        public async Task HandleAsync(Event @event, CancellationToken cancellationToken)
        {
            await client.UpdateAsync<Product, object>(
                "products",
                @event.Id,
                u =>
                    u.Doc(
                        new
                        {
                            Name = @event.Name,
                            Description = @event.Description,
                            Price = @event.Price,
                        }
                    ),
                cancellationToken
            );
        }
    }
}
