using Core.Messaging;

namespace Customer.API.Extensions;

public static class MessagingExtensions
{
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddMessageBroker();
        services.AddRabbitMqEventConsumer(typeof(Program).Assembly);

        return services;
    }
}
