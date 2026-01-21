using Core.Messaging;
using Core.Messaging.Options;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Notification.API.Clients.Customer;
using Notification.API.Clients.Customer.Interfaces;
using Notification.API.Configuration;
using Notification.API.Data;
using Notification.API.Services.Implementation;
using Notification.API.Services.Interfaces;

namespace Notification.API.Extensions;

public static class ServiceExtensions
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

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(Program).Assembly;
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddHttpContextAccessor();
        services.AddTransient<IEmailSender, SmtpEmailSender>();
        return services;
    }

    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        services.AddMessageBroker();

        return services;
    }
}
