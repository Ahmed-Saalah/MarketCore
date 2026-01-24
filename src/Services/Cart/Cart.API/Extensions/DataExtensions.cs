using Cart.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Cart.API.Extensions;

public static class DataExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<CartDbContext>(options => options.UseNpgsql(connectionString));
        var redisConnectionString = configuration.GetConnectionString("Redis");
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
        });
        services.AddScoped<ICartRepository, CachedCartRepository>();
        return services;
    }
}
