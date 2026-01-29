using Core.Messaging;

namespace Warehouse.API.Messages;

[MessageKey("Warehouse.InventoryUpdatedEvent")]
public sealed record InventoryUpdatedEvent(
    Guid InventoryId,
    Guid ProductId,
    Guid StoreId,
    int QuantityAdded,
    int NewQuantityOnHand,
    string ReferenceNumber,
    DateTime OccurredOn
);
