using Microsoft.EntityFrameworkCore;
using Store.API.Data;

namespace Store.API.Extensions;

public static class DataExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<StoreDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
