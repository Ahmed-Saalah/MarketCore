using System.Security.Claims;
using Cart.API.Data;
using Cart.API.Extensions;
using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using MediatR;

namespace Cart.API.Features;

public sealed class GetCart
{
    public sealed record Request(Guid? CartId, Guid? UserId) : IRequest<Response>;

    public sealed record CartDto(
        Guid Id,
        Guid? UserId,
        decimal TotalPrice,
        List<CartItemDto> Items
    );

    public sealed record CartItemDto(
        Guid ProductId,
        string ProductName,
        decimal UnitPrice,
        int Quantity,
        string? PictureUrl
    );

    public sealed class Response : Result<CartDto>
    {
        public static implicit operator Response(CartDto cartDto) => new() { Value = cartDto };

        public static implicit operator Response(DomainError error) => new() { Error = error };
    }

    public sealed class Handler(ICartRepository repository) : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken ct)
        {
            Entities.Cart? cart = null;

            if (request.UserId.HasValue)
            {
                cart = await repository.GetCartByUserIdAsync(request.UserId.Value, ct);
            }
            else if (request.CartId.HasValue)
            {
                cart = await repository.GetCartAsync(request.CartId.Value, ct);
            }

            if (cart is null)
            {
                return new CartDto(request.CartId ?? Guid.NewGuid(), request.UserId, 0, []);
            }

            var response = new CartDto(
                cart.Id,
                cart.UserId,
                cart.TotalPrice,
                cart.Items.Select(i => new CartItemDto(
                        i.ProductId,
                        i.ProductName,
                        i.UnitPrice,
                        i.Quantity,
                        i.PictureUrl
                    ))
                    .ToList()
            );

            return response;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "/api/carts",
                    async (Guid? cartId, IMediator mediator, ClaimsPrincipal user) =>
                    {
                        var userId = user.GetUserId();
                        var response = await mediator.Send(new Request(cartId, userId));
                        return response.ToHttpResult();
                    }
                )
                .WithTags("Cart")
                .WithName("GetCart")
                .WithSummary("Get the current user's cart or a specific guest cart");
        }
    }
}
