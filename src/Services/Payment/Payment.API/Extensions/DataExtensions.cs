using Microsoft.EntityFrameworkCore;
using Payment.API.Data;

namespace Payment.API.Extensions;

public static class DataExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<PaymentDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
