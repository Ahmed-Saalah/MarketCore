namespace Catalog.API.Messages.Products;

public record ProductActivatedEvent(Guid ProductId, DateTime Timestamp);
