namespace Cart.API.Data;

public interface ICartRepository
{
    Task<Entities.Cart?> GetCartAsync(Guid cartId, CancellationToken ct = default);
    Task<Entities.Cart?> GetCartByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Entities.Cart> StoreCartAsync(Entities.Cart cart, CancellationToken ct = default);
    Task<bool> ClearCartAsync(Guid cartId, CancellationToken ct = default);
}
