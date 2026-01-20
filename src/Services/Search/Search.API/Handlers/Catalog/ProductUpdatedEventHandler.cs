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
    ) : IRequest;

    public sealed class Handler(ElasticsearchClient client) : IRequestHandler<Event>
    {
        public async Task Handle(Event @event, CancellationToken cancellationToken)
        {
            await client.UpdateAsync<Product, object>(
                new UpdateRequest<Product, object>(index: "products", id: new Id(@event.Id))
                {
                    Doc = new
                    {
                        Name = @event.Name,
                        Description = @event.Description,
                        Price = @event.Price,
                    },
                },
                cancellationToken
            );
        }
    }
}
