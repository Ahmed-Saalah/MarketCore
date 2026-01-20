namespace Cart.API.Messages;

public sealed class CartClearedEvent(Guid CartId, Guid? UserId, Guid? StoreId, DateTime Timestamp);
