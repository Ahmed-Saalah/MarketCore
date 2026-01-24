namespace Cart.API.Messages;

public sealed class CartClearedEvent(Guid CartId, Guid? UserId, DateTime Timestamp);
