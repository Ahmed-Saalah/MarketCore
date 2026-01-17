using System.Globalization;
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

    private static readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        SlidingExpiration = TimeSpan.FromHours(2),
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

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(cartFromDb, _jsonOptions),
            _cacheOptions,
            ct
        );

        return cartFromDb;
    }

    public async Task<Entities.Cart?> GetCartByUserIdAsync(
        Guid userId,
        CancellationToken ct = default
    )
    {
        string userPointerKey = $"user:{userId}";
        var cachedCartId = await _cache.GetStringAsync(userPointerKey, ct);
        if (!string.IsNullOrEmpty(cachedCartId) && Guid.TryParse(cachedCartId, out var cartId))
        {
            var cart = await GetCartAsync(cartId, ct);
            if (cart is not null)
                return cart;
        }

        var cartFromDb = await _dbContext
            .Carts.Include(c => c.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

        if (cartFromDb is null)
        {
            return null;
        }

        await StoreCartAsync(cartFromDb, ct);
        return cartFromDb;
    }

    public async Task<Entities.Cart> StoreCartAsync(
        Entities.Cart cart,
        CancellationToken ct = default
    )
    {
        _dbContext.Carts.Update(cart);
        await _dbContext.SaveChangesAsync(ct);

        string cartKey = $"cart:{cart.Id}";
        await _cache.SetStringAsync(
            cartKey,
            JsonSerializer.Serialize(cart, _jsonOptions),
            _cacheOptions,
            ct
        );

        if (cart.UserId.HasValue)
        {
            string userPointerKey = $"user:{cart.UserId}";
            await _cache.SetStringAsync(userPointerKey, cart.Id.ToString(), _cacheOptions, ct);
        }

        return cart;
    }

    public async Task<bool> ClearCartAsync(Guid cartId, CancellationToken ct = default)
    {
        var cart = await _dbContext
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

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(cart, _jsonOptions),
            _cacheOptions,
            ct
        );

        return true;
    }
}
