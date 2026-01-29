using Core.Messaging;

namespace Order.API.Messages;

[MessageKey("Order.OrderCanceledEvent")]
public sealed record OrderCanceledEvent(
    Guid OrderId,
    Guid UserId,
    Guid StoreId,
    decimal Total,
    string Reason,
    List<OrderCanceledItemDto> Items
);

public sealed record OrderCanceledItemDto(Guid ProductId, int Quantity);
