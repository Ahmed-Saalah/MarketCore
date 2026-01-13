namespace Catalog.API.Messages.Products;

public record ProductDeactivatedEvent(Guid ProductId, DateTime Timestamp);
