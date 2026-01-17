namespace Cart.API.Messages;

public sealed record CartItemUpdatedEvent(
    Guid CartId,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    string? PictureUrl
);
