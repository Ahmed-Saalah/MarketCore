using Elastic.Clients.Elasticsearch;
using MediatR;
using Search.API.Models;

namespace Search.API.Handlers.Catalog;

public sealed class ProductDeactivatedEventHandler
{
    public sealed record Event(Guid ProductId, DateTime Timestamp) : IRequest;

    public sealed class Handler(ElasticsearchClient client) : IRequestHandler<Event>
    {
        public async Task Handle(Event @event, CancellationToken cancellationToken)
        {
            await client.UpdateAsync<Product, object>(
                new UpdateRequest<Product, object>(index: "products", id: new Id(@event.ProductId))
                {
                    Doc = new { IsActive = false },
                },
                cancellationToken
            );
        }
    }
}
