namespace Catalog.API.Messages.Products;

public sealed record ProductUpdatedEvent(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Sku,
    Guid StoreId
);
