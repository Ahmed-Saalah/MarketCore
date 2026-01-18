using System.Security.Claims;
using Cart.API.Data;
using Cart.API.Extensions;
using Cart.API.Messages;
using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using Core.Messaging;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Cart.API.Features;

public sealed class RemoveItem
{
    public sealed record Request(Guid CartId, Guid? UserId, Guid ProductId) : IRequest<Response>;

    public sealed class Response : Result<bool>
    {
        public static implicit operator Response(bool success) => new() { Value = success };

        public static implicit operator Response(DomainError error) => new() { Error = error };
    }

    public sealed class Handler(ICartRepository repository, IEventPublisher eventPublisher)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken ct)
        {
            Entities.Cart? cart = await repository.GetCartAsync(request.CartId, ct);
            if (cart is null && request.UserId.HasValue)
            {
                cart = await repository.GetCartByUserIdAsync(request.UserId.Value, ct);
            }

            if (cart is null)
            {
                return new NotFound("Cart not found");
            }

            var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
            if (item is null)
            {
                return new NotFound("Cart item not found");
            }

            cart.Items.Remove(item);
            cart.UpdatedAt = DateTime.UtcNow;
            await repository.StoreCartAsync(cart, ct);

            await eventPublisher.PublishAsync(
                new CartItemRemovedEvent(
                    cart.Id,
                    item.ProductId,
                    item.ProductName,
                    item.UnitPrice,
                    item.Quantity,
                    item.PictureUrl
                ),
                "Cart.CartItemRemovedEvent",
                ct
            );

            return true;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapDelete(
                    "/api/cart/{cartId}/items/{productId:guid}",
                    async (
                        [FromRoute] Guid cartId,
                        [FromRoute] Guid productId,
                        IMediator mediator,
                        ClaimsPrincipal user
                    ) =>
                    {
                        var response = await mediator.Send(
                            new Request(cartId, user.GetUserId(), productId)
                        );
                        return response.ToHttpResult();
                    }
                )
                .WithTags("Cart")
                .WithName("RemoveCartItem");
        }
    }
}
