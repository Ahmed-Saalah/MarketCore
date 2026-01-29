using Core.Messaging;

namespace Catalog.API.Messages.Products;

[MessageKey("Catalog.ProductDeactivatedEvent")]
public record ProductDeactivatedEvent(Guid ProductId, DateTime Timestamp);
