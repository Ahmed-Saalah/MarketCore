using Auth.API.Handlers;
using Core.Messaging;

namespace Auth.API.Extensions;

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
            (
                typeof(StoreCreatedEventHandler.Event),
                typeof(StoreCreatedEventHandler.Handler),
                "Store.StoreCreatedEvent"
            ),
            (
                typeof(CustomerCreatedEventHandler.Event),
                typeof(CustomerCreatedEventHandler.Handler),
                "Customer.CustomerCreatedEvent"
            )
        );

        return services;
    }
}
