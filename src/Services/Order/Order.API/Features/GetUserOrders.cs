using System.Security.Claims;
using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.API.Data;
using Order.API.Extensions;

namespace Order.API.Features;

public sealed class GetUserOrders
{
    public sealed record Request(Guid UserId) : IRequest<Response>;

    public sealed class Response : Result<ResponseDto[]>
    {
        public static implicit operator Response(ResponseDto[] dto) => new() { Value = dto };

        public static implicit operator Response(DomainError error) => new() { Error = error };
    }

    public sealed record ResponseDto(
        Guid OrderId,
        Guid StoreId,
        string Status,
        string OrderNumber,
        decimal Subtotal,
        decimal Total,
        DateTime CreateAt,
        OrderItemDto[] OrderItems
    );

    public sealed record OrderItemDto(
        Guid ProductId,
        string ProductName,
        decimal UnitPrice,
        int Quantity,
        decimal TotalPrice
    );

    public sealed class Handler(OrderDbContext dbContext) : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var orders = await dbContext
                .Orders.Include(o => o.Items)
                .Where(o => o.UserId == request.UserId)
                .Select(o => new ResponseDto(
                    o.Id,
                    o.StoreId,
                    o.Status,
                    o.OrderNumber,
                    o.Subtotal,
                    o.Total,
                    o.CreatedAt,
                    o.Items.Select(oi => new OrderItemDto(
                            oi.ProductId,
                            oi.ProductName,
                            oi.UnitPrice,
                            oi.Quantity,
                            oi.TotalPrice
                        ))
                        .ToArray()
                ))
                .ToArrayAsync(cancellationToken);

            return orders;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "api/orders/users/me",
                    async (IMediator mediator, ClaimsPrincipal user) =>
                    {
                        if (user.GetUserId() is not { } userId)
                        {
                            return Results.Unauthorized();
                        }

                        var response = await mediator.Send(new Request(userId));
                        return response.ToHttpResult();
                    }
                )
                .WithTags("Orders")
                .RequireAuthorization("Customer");
        }
    }
}
