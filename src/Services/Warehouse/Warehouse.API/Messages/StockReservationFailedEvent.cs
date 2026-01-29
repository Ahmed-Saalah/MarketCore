using Core.Messaging;

namespace Warehouse.API.Messages;

[MessageKey("Warehouse.StockReservationFailedEvent")]
public sealed record StockReservationFailedEvent(
    Guid OrderId,
    Guid StoreId,
    string Reason,
    List<Item> Items
);

public sealed record Item(Guid ProductId, int Quantity);
