namespace Warehouse.API.Messages;

public sealed record InventoryUpdatedEvent(
    Guid InventoryId,
    Guid ProductId,
    Guid StoreId,
    int QuantityAdded,
    int NewQuantityOnHand,
    string ReferenceNumber,
    DateTime OccurredOn
);
