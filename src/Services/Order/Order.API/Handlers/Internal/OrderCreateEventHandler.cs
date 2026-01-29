using Core.Messaging;
using Order.API.Messages;

namespace Order.API.Handlers.Internal;

public sealed class OrderCreateEventHandler
{
    public sealed class Handler(IEventPublisher commandPublisher) : IEventHandler<OrderCreatedEvent>
    {
        public async Task HandleAsync(OrderCreatedEvent @event, CancellationToken cancellationToken)
        {
            await commandPublisher.PublishAsync(
                new ReserveStockCommand(
                    @event.OrderId,
                    @event.StoreId,
                    @event.Items.Select(i => new OrderItemDto(i.ProductId, i.Quantity)).ToList()
                ),
                cancellationToken
            );
        }
    }
}
