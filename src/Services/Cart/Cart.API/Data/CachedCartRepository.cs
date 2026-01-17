using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Cart.API.Data;

public class CachedCartRepository : ICartRepository
{
    private readonly IDistributedCache _cache;
    private readonly CartDbContext _dbContext;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.Preserve,
        WriteIndented = false,
    };

    public CachedCartRepository(IDistributedCache cache, CartDbContext dbContext)
    {
        _cache = cache;
        _dbContext = dbContext;
    }

    public async Task<Entities.Cart?> GetCartAsync(Guid cartId, CancellationToken ct = default)
    {
        string cacheKey = $"cart:{cartId}";
        var cachedData = await _cache.GetStringAsync(cacheKey, ct);

        if (!string.IsNullOrEmpty(cachedData))
        {
            return JsonSerializer.Deserialize<Entities.Cart>(cachedData, _jsonOptions);
        }

        var cartFromDb = await _dbContext
            .Carts.Include(c => c.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cartId, ct);

        if (cartFromDb is null)
        {
            return null;
        }

        var serializedCart = JsonSerializer.Serialize(cartFromDb);

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(cartFromDb, _jsonOptions),
            new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromHours(2) },
            ct
        );

        return cartFromDb;
    }

    public async Task<Entities.Cart> StoreCartAsync(
        Entities.Cart cart,
        CancellationToken ct = default
    )
    {
        var exists = await _dbContext.Carts.AnyAsync(c => c.Id == cart.Id, ct);
        if (exists)
        {
            _dbContext.Carts.Update(cart);
        }
        else
        {
            await _dbContext.Carts.AddAsync(cart, ct);
        }
        await _dbContext.SaveChangesAsync(ct);

        string cacheKey = $"cart:{cart.Id}";
        var serializedCart = JsonSerializer.Serialize(cart);

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(cart, _jsonOptions),
            new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromHours(2) },
            ct
        );
        return cart;
    }

    public async Task<bool> ClearCartAsync(Guid cartId, CancellationToken ct = default)
    {
        var cart = await dbContext
            .Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cartId, ct);

        if (cart is null)
        {
            return false;
        }

        cart.Items.Clear();
        cart.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        string cacheKey = $"cart:{cartId}";
        await _cache.RemoveAsync(cacheKey, ct);

        return true;
    }
}
