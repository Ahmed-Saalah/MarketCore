using Auth.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Auth.API.Extensions;

public static class DataExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}
