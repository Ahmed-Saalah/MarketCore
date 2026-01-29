using Core.Messaging;

namespace Cart.API.Messages;

[MessageKey("Cart.CartItemUpdatedEvent")]
public sealed record CartItemUpdatedEvent(
    Guid CartId,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    string? PictureUrl
);
