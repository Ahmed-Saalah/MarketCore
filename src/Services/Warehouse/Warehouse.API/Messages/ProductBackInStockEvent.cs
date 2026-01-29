using Core.Messaging;

namespace Warehouse.API.Messages;

[MessageKey("Warehouse.ProductBackInStockEvent")]
public sealed record ProductBackInStockEvent(
    Guid ProductId,
    Guid StoreId,
    Guid InventoryId,
    DateTime Timestamp
);
