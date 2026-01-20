using System.Security.Claims;
using Cart.API.Data;
using Cart.API.Entities;
using Cart.API.Extensions;
using Cart.API.Messages;
using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using Core.Messaging;
using FluentValidation;
using MediatR;

namespace Cart.API.Features;

public sealed class AddItem
{
    public sealed record AddItemDto(
        Guid ProductId,
        string ProductName,
        decimal UnitPrice,
        int Quantity,
        string? PictureUrl
    );

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

    public sealed record Request(Guid? CartId, Guid? UserId, Guid StoreId, AddItemDto Item)
        : IRequest<Response>;

    public sealed class Response : Result<CartDto>
    {
        public static implicit operator Response(CartDto id) => new() { Value = id };

        public static implicit operator Response(DomainError error) => new() { Error = error };
    }

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Item.ProductId).NotEmpty();
            RuleFor(x => x.Item.Quantity).GreaterThan(0);
            RuleFor(x => x.Item.UnitPrice).GreaterThanOrEqualTo(0);
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
                cart = new Entities.Cart
                {
                    Id = request.CartId ?? Guid.NewGuid(),
                    UserId = request.UserId,
                    StoreId = request.StoreId,
                    Items = [],
                };
            }

            var existingItem = cart.Items.FirstOrDefault(i =>
                i.ProductId == request.Item.ProductId
            );

            if (existingItem is not null)
            {
                existingItem.Quantity += request.Item.Quantity;
                existingItem.UnitPrice = request.Item.UnitPrice;
            }
            else
            {
                cart.Items.Add(
                    new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = request.Item.ProductId,
                        ProductName = request.Item.ProductName,
                        UnitPrice = request.Item.UnitPrice,
                        Quantity = request.Item.Quantity,
                        PictureUrl = request.Item.PictureUrl,
                        AddedAt = DateTime.UtcNow,
                    }
                );
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await repository.StoreCartAsync(cart, ct);

            var cartItem = existingItem ?? cart.Items.Last();
            await eventPublisher.PublishAsync(
                new CartItemAddedEvent(
                    cart.Id,
                    cartItem.ProductId,
                    cartItem.ProductName,
                    cartItem.UnitPrice,
                    cartItem.Quantity,
                    cartItem.PictureUrl
                ),
                "Cart.AddCartItemAddedEvent",
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
            app.MapPost(
                    "/api/carts/items",
                    async (
                        AddItemDto dto,
                        Guid? cartId,
                        Guid storeId,
                        IMediator mediator,
                        ClaimsPrincipal user
                    ) =>
                    {
                        var result = await mediator.Send(
                            new Request(cartId, user.GetUserId(), storeId, dto)
                        );
                        return result.ToHttpResult();
                    }
                )
                .WithTags("Cart")
                .WithName("AddItemToCart");
        }
    }
}
