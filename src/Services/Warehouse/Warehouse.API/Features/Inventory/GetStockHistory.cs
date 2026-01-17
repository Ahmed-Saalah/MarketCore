using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Warehouse.API.Data;

namespace Warehouse.API.Features.Inventory;

public sealed class GetStockHistory
{
    public sealed record Request(Guid ProductId) : IRequest<Response>;

    public sealed class Response : Result<ResponseDto[]>
    {
        public static implicit operator Response(ResponseDto[] success) =>
            new() { Value = success };

        public static implicit operator Response(DomainError error) => new() { Error = error };
    }

    public sealed record ResponseDto(
        Guid Id,
        string Type,
        int QuantityChanged,
        string ReferenceId,
        DateTime OccurredOn
    );

    internal sealed class Handler(WarehouseDbContext dbContext) : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken ct)
        {
            var inventory = await dbContext
                .Inventory.AsNoTracking()
                .FirstOrDefaultAsync(i => i.ProductId == request.ProductId, ct);

            if (inventory is null)
            {
                return new NotFound("No inventory record found for this product.");
            }

            var history = await dbContext
                .StockTransactions.AsNoTracking()
                .Where(t => t.InventoryId == inventory.Id)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new ResponseDto(
                    t.Id,
                    t.Type.ToString(),
                    t.QuantityChanged,
                    t.ReferenceId,
                    t.CreatedAt
                ))
                .ToArrayAsync(ct);

            return history;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "/api/inventory/{productId:guid}/history",
                    async (Guid productId, IMediator mediator) =>
                    {
                        var response = await mediator.Send(new Request(productId));
                        return response.ToHttpResult();
                    }
                )
                .WithTags("Inventory")
                .WithName("GetStockHistory")
                .WithSummary("View the audit log of all stock changes for a product");
        }
    }
}
