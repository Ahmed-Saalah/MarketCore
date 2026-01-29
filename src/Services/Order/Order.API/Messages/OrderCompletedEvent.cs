using Core.Messaging;

namespace Order.API.Messages;

[MessageKey("Order.OrderCompletedEvent")]
public sealed record OrderCompletedEvent(
    Guid OrderId,
    Guid UserId,
    Guid StoreId,
    decimal Total,
    List<OrderCompletedItemDto> Items
);

public sealed record OrderCompletedItemDto(Guid ProductId, int Quantity);
