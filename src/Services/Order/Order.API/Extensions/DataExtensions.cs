using Microsoft.EntityFrameworkCore;
using Order.API.Data;

namespace Order.API.Extensions;

public static class DataExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<OrderDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
