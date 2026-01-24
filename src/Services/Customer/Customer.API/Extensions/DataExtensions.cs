using Customer.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Customer.API.Extensions;

public static class DataExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<CustomerDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
