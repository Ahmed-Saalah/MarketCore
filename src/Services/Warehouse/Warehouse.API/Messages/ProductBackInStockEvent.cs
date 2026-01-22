namespace Warehouse.API.Messages;

public sealed record ProductBackInStockEvent(
    Guid ProductId,
    Guid StoreId,
    Guid InventoryId,
    DateTime Timestamp
);
