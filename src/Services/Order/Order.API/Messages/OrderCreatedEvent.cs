using Core.Messaging;

namespace Order.API.Messages;

[MessageKey("Order.OrderCreatedEvent")]
public record OrderCreatedEvent(
    Guid OrderId,
    Guid StoreId,
    Guid UserId,
    List<OrderCreatedItem> Items,
    DateTime CreatedAt
);

public record OrderCreatedItem(Guid ProductId, int Quantity);
