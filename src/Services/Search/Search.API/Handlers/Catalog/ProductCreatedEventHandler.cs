using Elastic.Clients.Elasticsearch;
using MediatR;
using Search.API.Models;

namespace Search.API.Handlers.Catalog;

public sealed class ProductCreatedEventHandler
{
    public sealed record Event(
        Guid ProductId,
        Guid StoreId,
        string Sku,
        string Name,
        string Description,
        decimal Price,
        string? PictureUrl,
        Guid CategoryId,
        string? CategoryName,
        DateTime CreatedAt
    ) : IRequest;

    public sealed class Handler(ElasticsearchClient client) : IRequestHandler<Event>
    {
        public async Task Handle(Event @event, CancellationToken cancellationToken)
        {
            var product = new Product
            {
                Id = @event.ProductId,
                Name = @event.Name,
                Description = @event.Description,
                Price = @event.Price,
                PictureUrl = @event.PictureUrl,
                StoreId = @event.StoreId,
                IsActive = true,
            };

            await client.IndexAsync(product, idx => idx.Index("products"), cancellationToken);
        }
    }
}
