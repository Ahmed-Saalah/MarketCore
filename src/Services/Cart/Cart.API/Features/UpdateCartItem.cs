using System.Security.Claims;
using Cart.API.Data;
using Cart.API.Extensions;
using Cart.API.Messages;
using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using Core.Messaging;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Cart.API.Features;

public sealed class UpdateCartItem
{
    public sealed record RequestDto(Guid ProductId, int Quantity);

    public record Request(Guid CartId, Guid? UserId, RequestDto Dto) : IRequest<Response>;

    public sealed record CartDto(
        Guid Id,
        Guid? UserId,
        Guid StoreId,
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
        public static implicit operator Response(CartDto cart) => new() { Value = cart };

        public static implicit operator Response(DomainError error) => new() { Error = error };
    }

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.CartId).NotEmpty();
            RuleFor(x => x.Dto.ProductId).NotEmpty();
            RuleFor(x => x.Dto.Quantity).GreaterThan(0);
        }
    }

    public sealed class Handler(
        ICartRepository repository,
        IValidator<Request> validator,
        IEventPublisher eventPublisher
    ) : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken ct)
        {
            var validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return new ValidationError(validationResult.Errors);
            }

            var cart = await repository.GetCartAsync(request.CartId, ct);

            if (cart is null)
            {
                return new NotFound("Cart not found");
            }

            var item = cart.Items.FirstOrDefault(i => i.ProductId == request.Dto.ProductId);
            if (item is null)
            {
                return new NotFound("Cart item not found");
            }

            item.Quantity = request.Dto.Quantity;
            cart.UpdatedAt = DateTime.UtcNow;
            await repository.StoreCartAsync(cart, ct);

            await eventPublisher.PublishAsync(
                new CartItemUpdatedEvent(
                    cart.Id,
                    item.ProductId,
                    item.ProductName,
                    item.UnitPrice,
                    item.Quantity,
                    item.PictureUrl
                ),
                "Cart.CartItemUpdatedEvent",
                ct
            );

            return new CartDto(
                cart.Id,
                cart.UserId,
                cart.StoreId,
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
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPut(
                    "/api/carts/{cartId}/items",
                    async (
                        [FromRoute] Guid cartId,
                        [FromBody] RequestDto data,
                        IMediator mediator,
                        ClaimsPrincipal user
                    ) =>
                    {
                        var result = await mediator.Send(
                            new Request(cartId, user.GetUserId(), data)
                        );
                        return result.ToHttpResult();
                    }
                )
                .WithTags("Cart")
                .WithName("UpdateCartItem");
        }
    }
}
