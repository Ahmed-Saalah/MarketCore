using Core.Messaging;

namespace Warehouse.API.Messages;

[MessageKey("Warehouse.StockReservedEvent")]
public sealed record StockReservedEvent(Guid OrderId, Guid StoreId);
