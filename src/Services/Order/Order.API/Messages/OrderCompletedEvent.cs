namespace Order.API.Messages;

public sealed class OrderCompletedEvent(Guid OrderId, Guid UserId, Guid StoreId, decimal Total);
