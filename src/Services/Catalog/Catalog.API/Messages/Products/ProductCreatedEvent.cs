using Core.Messaging;

namespace Catalog.API.Messages.Products;

[MessageKey("Catalog.ProductCreatedEvent")]
public record ProductCreatedEvent(
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
);
