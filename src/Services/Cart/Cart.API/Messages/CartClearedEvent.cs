using Core.Messaging;

namespace Cart.API.Messages;

[MessageKey("Cart.CartClearedEvent")]
public sealed class CartClearedEvent(Guid CartId, Guid? UserId, DateTime Timestamp);
