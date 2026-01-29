using Core.Messaging;

namespace Catalog.API.Messages.Products;

[MessageKey("Catalog.ProductUpdatedEvent")]
public sealed record ProductUpdatedEvent(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Sku,
    Guid StoreId
);
