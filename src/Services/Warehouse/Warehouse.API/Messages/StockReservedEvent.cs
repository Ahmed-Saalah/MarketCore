namespace Warehouse.API.Messages;

public sealed record StockReservedEvent(Guid OrderId, Guid StoreId);
