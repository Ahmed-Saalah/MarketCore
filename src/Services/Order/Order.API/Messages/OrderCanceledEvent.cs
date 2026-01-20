namespace Order.API.Messages;

public sealed class OrderCanceledEvent(
    Guid OrderId,
    Guid UserId,
    Guid StoreId,
    decimal Total,
    string Reason
);
