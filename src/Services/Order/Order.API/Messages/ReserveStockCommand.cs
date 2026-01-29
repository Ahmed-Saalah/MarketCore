using Core.Messaging;

namespace Order.API.Messages;

[MessageKey("Order.ReserveStockCommand")]
public record ReserveStockCommand(Guid OrderId, Guid StoreId, List<OrderItemDto> Items);

public record OrderItemDto(Guid ProductId, int Quantity);
