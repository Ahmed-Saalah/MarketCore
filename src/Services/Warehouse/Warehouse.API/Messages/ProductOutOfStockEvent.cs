using Core.Messaging;

namespace Warehouse.API.Messages;

[MessageKey("Warehouse.ProductOutOfStockEvent")]
public sealed record ProductOutOfStockEvent(
    Guid ProductId,
    Guid StoreId,
    Guid InventoryId,
    DateTime Timestamp
);
