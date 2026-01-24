using Catalog.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Extensions;

public static class DataExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<CatalogDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
