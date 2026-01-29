using Core.Messaging;

namespace Catalog.API.Messages.Products;

[MessageKey("Catalog.ProductActivatedEvent")]
public record ProductActivatedEvent(Guid ProductId, DateTime Timestamp);
