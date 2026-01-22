namespace Warehouse.API.Messages;

public sealed record ProductOutOfStockEvent(
    Guid ProductId,
    Guid StoreId,
    Guid InventoryId,
    DateTime Timestamp
);
