using Core.Messaging;

namespace Cart.API.Messages;

[MessageKey("Cart.CartItemRemovedEvent")]
public sealed record CartItemRemovedEvent(
    Guid CartId,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    string? PictureUrl
);
