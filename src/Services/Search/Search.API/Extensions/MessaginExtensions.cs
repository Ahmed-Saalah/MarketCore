using Core.Messaging;

namespace Search.API.Extensions;

public static class MessaginExtensions
{
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddRabbitMqEventConsumer(typeof(Program).Assembly);

        return services;
    }
}
