using Core.Messaging;

namespace Cart.API.Messages;

[MessageKey("Cart.AddCartItemAddedEvent")]
public sealed record CartItemAddedEvent(
    Guid CartId,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    string? PictureUrl
);
