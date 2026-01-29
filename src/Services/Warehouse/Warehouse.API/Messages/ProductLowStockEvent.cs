using Core.Messaging;

namespace Warehouse.API.Messages;

[MessageKey("Warehouse.ProductLowStockEvent")]
public sealed record ProductLowStockEvent(
    Guid ProductId,
    Guid StoreId,
    Guid InventoryId,
    DateTime Timestamp
);
