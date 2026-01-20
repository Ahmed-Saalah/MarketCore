namespace Order.API.Messages;

public sealed class OrderCanceledEvent(
    Guid OrderId,
    Guid UserId,
    Guid StoreId,
    decimal Total,
    string Reason,
    List<OrderCanceledItemDto> Items
);

public sealed record OrderCanceledItemDto(Guid ProductId, int Quantity);
