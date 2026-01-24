using Core.Messaging;
using Customer.API.Handlers;

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
