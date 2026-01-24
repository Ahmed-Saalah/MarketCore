using Microsoft.EntityFrameworkCore;
using Warehouse.API.Data;

namespace Warehouse.API.Extensions;

public static class DataExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<WarehouseDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
