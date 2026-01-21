using Cart.API.Data;
using Core.Messaging;

namespace Cart.API.Handler.Order;

public sealed class OrderCompletedEventHandler
{
    public sealed class Handler(ICartRepository repository) : IEventHandler<Event>
    {
        public async Task HandleAsync(Event @event, CancellationToken cancellationToken = default)
        {
            var cart = await repository.GetCartByUserIdAsync(@event.UserId, cancellationToken);
            await repository.ClearCartAsync(cart.Id, cancellationToken);
        }
    }

    public sealed record Event(
        Guid OrderId,
        Guid UserId,
        Guid StoreId,
        decimal Total,
        List<OrderCompletedItemDto> Items
    );

    public sealed record OrderCompletedItemDto(Guid ProductId, int Quantity);
}
