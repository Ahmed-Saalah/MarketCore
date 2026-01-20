using System.Security.Claims;
using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using Core.Messaging;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Order.API.Constants;
using Order.API.Data;
using Order.API.Entities;
using Order.API.Extensions;
using Order.API.Helpers;
using Order.API.Messages;

namespace Order.API.Features;

public sealed class CreateOrder
{
    public record CreateOrderDto(Guid StoreId, List<CreateOrderItemDto> Items);

    public record CreateOrderItemDto(
        Guid ProductId,
        string ProductName,
        string Sku,
        decimal UnitPrice,
        int Quantity
    );

    public record Request(Guid UserId, Guid StoreId, List<CreateOrderItemDto> Items)
        : IRequest<Response>;

    public record ResponseDto(Guid OrderId);

    public class Response : Result<ResponseDto>
    {
        public static implicit operator Response(ResponseDto dto) => new() { Value = dto };

        public static implicit operator Response(DomainError error) => new() { Error = error };
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.StoreId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Items).NotEmpty().WithMessage("Order must contain at least one item.");

            RuleForEach(x => x.Items)
                .ChildRules(items =>
                {
                    items.RuleFor(i => i.ProductId).NotEmpty();
                    items.RuleFor(i => i.Quantity).GreaterThan(0);
                    items.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0);
                });
        }
    }

    public sealed class Handler(OrderDbContext dbContext, IEventPublisher eventPublisher)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var order = new Entities.Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = OrderNumberGenerator.Generate(),
                StoreId = request.StoreId,
                UserId = request.UserId,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                Subtotal = request.Items.Sum(x => x.UnitPrice * x.Quantity),
                Tax = 0,
                ShippingFee = 0,
            };

            order.Total = order.Subtotal + order.Tax + order.ShippingFee;

            foreach (var item in request.Items)
            {
                order.Items.Add(
                    new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Sku = item.Sku,
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity,
                    }
                );
            }

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync(cancellationToken);

            await eventPublisher.PublishAsync(
                new OrderCreatedEvent(
                    order.Id,
                    order.StoreId,
                    order.UserId,
                    order.Items.Select(i => new OrderCreatedItem(i.ProductId, i.Quantity)).ToList(),
                    order.CreatedAt
                ),
                "Order.OrderCreatedEvent",
                cancellationToken
            );

            return new ResponseDto(order.Id);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                "api/orders",
                async ([FromBody] CreateOrderDto data, ClaimsPrincipal user, IMediator mediator) =>
                {
                    if (user.GetUserId() is not { } userId)
                    {
                        return Results.Unauthorized();
                    }

                    var response = await mediator.Send(
                        new Request(userId, data.StoreId, data.Items)
                    );

                    return response.ToHttpResult();
                }
            );
        }
    }
}
