namespace Warehouse.API.Messages;

public sealed record StockReservationFailedEvent(
    Guid OrderId,
    Guid StoreId,
    string Reason,
    List<Item> Items
);

public sealed record Item(Guid ProductId, int Quantity);
