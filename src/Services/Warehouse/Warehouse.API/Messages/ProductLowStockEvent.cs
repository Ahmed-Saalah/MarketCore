namespace Warehouse.API.Messages;

public sealed record ProductLowStockEvent(
    Guid ProductId,
    Guid StoreId,
    Guid InventoryId,
    DateTime Timestamp
);
