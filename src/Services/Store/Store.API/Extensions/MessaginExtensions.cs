using Core.Messaging;
using Store.API.Handlers;

namespace Store.API.Extensions;

public static class MessaginExtensions
{
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        services.AddMessageBroker();

        services.AddRabbitMqEventConsumer(
            events: (
                typeof(UserCreatedEventHandler.Event),
                typeof(UserCreatedEventHandler.Handler),
                "Auth.UserCreatedEvent"
            )
        );

        return services;
    }
}
