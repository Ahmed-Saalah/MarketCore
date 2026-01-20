namespace Order.API.Messages;

public record OrderCreatedEvent(
    Guid OrderId,
    Guid StoreId,
    Guid UserId,
    List<OrderCreatedItem> Items,
    DateTime CreatedAt
);

public record OrderCreatedItem(Guid ProductId, int Quantity);
