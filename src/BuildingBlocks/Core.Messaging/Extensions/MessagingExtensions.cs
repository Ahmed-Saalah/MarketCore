using System.Reflection;
using Core.Messaging.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddMessageBroker(this IServiceCollection services)
    {
        services.AddSingleton<IEventPublisher>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

            return EventPublisher
                .CreateAsync(options.Host, options.User, options.Password, options.Exchange)
                .GetAwaiter()
                .GetResult();
        });

        return services;
    }

    public static IServiceCollection AddRabbitMqEventConsumer(
        this IServiceCollection services,
        Assembly assembly
    )
    {
        var routingKeyMap = new Dictionary<string, Type>();

        var handlerTypes = assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Select(t => new
            {
                HandlerType = t,
                InterfaceType = t.GetInterfaces()
                    .FirstOrDefault(i =>
                        i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>)
                    ),
            })
            .Where(x => x.InterfaceType != null)
            .ToList();

        foreach (var item in handlerTypes)
        {
            var eventType = item.InterfaceType!.GetGenericArguments()[0];
            var attribute = eventType.GetCustomAttribute<MessageKeyAttribute>();
            if (attribute is null)
            {
                throw new InvalidOperationException(
                    $"Event '{eventType.Name}' cannot be registered because it is missing the [MessageKey] attribute."
                );
            }

            routingKeyMap[attribute.Key] = eventType;
            services.AddScoped(item.InterfaceType, item.HandlerType);
        }

        services.AddHostedService(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

            return new RabbitMqConsumerService(
                sp.GetRequiredService<ILogger<RabbitMqConsumerService>>(),
                sp,
                options.Host,
                options.User,
                options.Password,
                options.Exchange,
                options.Queue,
                routingKeyMap
            );
        });

        return services;
    }
}
