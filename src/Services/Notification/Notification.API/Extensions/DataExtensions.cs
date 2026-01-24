using Microsoft.EntityFrameworkCore;
using Notification.API.Clients.Customer;
using Notification.API.Clients.Customer.Interfaces;
using Notification.API.Configuration;
using Notification.API.Data;

namespace Notification.API.Extensions;

public static class DataExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(connectionString)
        );

        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));

        services.AddHttpClient<ICustomerApiClient, CustomerApiClient>(client =>
        {
            var url = configuration["ApiUrls:CustomerApi"];
            client.BaseAddress = new Uri(url);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        return services;
    }
}
