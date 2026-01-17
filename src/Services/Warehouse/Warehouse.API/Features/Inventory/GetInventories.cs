using System.Security.Claims;
using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warehouse.API.Data;
using Warehouse.API.Extensions;

namespace Warehouse.API.Features.Inventory;

public sealed class GetInventories
{
    public sealed record Request(Guid StoreId, RequestDto Dto) : IRequest<Response>;

    public sealed record RequestDto(int PageIndex = 1, int PageSize = 10);

    public sealed class Response : Result<PaginatedResult<ResponseDto>>
    {
        public static implicit operator Response(PaginatedResult<ResponseDto> success) =>
            new() { Value = success };

        public static implicit operator Response(DomainError error) => new() { Error = error };
    }

    public record ResponseDto(Guid ProductId, string Sku, int QuantityOnHand, int ReservedQuantity);

    internal sealed class Handler(WarehouseDbContext dbContext) : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken ct)
        {
            var query = dbContext.Inventory.AsNoTracking().Where(i => i.StoreId == request.StoreId);

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderBy(i => i.Sku)
                .Skip((request.Dto.PageIndex - 1) * request.Dto.PageSize)
                .Take(request.Dto.PageSize)
                .Select(i => new ResponseDto(
                    i.ProductId,
                    i.Sku,
                    i.QuantityOnHand,
                    i.ReservedQuantity
                ))
                .ToListAsync(ct);

            return new PaginatedResult<ResponseDto>(
                request.Dto.PageIndex,
                request.Dto.PageSize,
                totalCount,
                items
            );
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "/api/inventory/me",
                    async (
                        [FromQuery] int? page,
                        [FromQuery] int? size,
                        IMediator mediator,
                        ClaimsPrincipal user
                    ) =>
                    {
                        if (user.GetStoreId() is not { } storeId)
                        {
                            return Results.Unauthorized();
                        }

                        var response = await mediator.Send(
                            new Request(storeId, new RequestDto(page ?? 1, size ?? 10))
                        );

                        return response.ToHttpResult();
                    }
                )
                .RequireAuthorization("Seller", "Admin")
                .WithTags("Inventory")
                .WithName("GetInventories")
                .WithSummary("Get a paged list of all inventory items");
        }
    }
}
