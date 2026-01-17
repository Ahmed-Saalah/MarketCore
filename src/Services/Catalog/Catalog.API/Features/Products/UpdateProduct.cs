using System.Security.Claims;
using Catalog.API.Data;
using Catalog.API.Entities;
using Catalog.API.Entities.Products;
using Catalog.API.Extensions;
using Catalog.API.Messages.Products;
using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using Core.Messaging;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Features.Products;

public static class UpdateProduct
{
    public sealed record ImageDto(string ImageUrl, bool IsPrimary);

    public sealed record AttributeDto(string Key, string Value);

    public sealed record RequestDto(
        string Name,
        string Description,
        decimal Price,
        string Sku,
        List<ImageDto> Images,
        List<AttributeDto> Attributes
    );

    public sealed record Request(Guid Id, Guid StoreId, RequestDto Data) : IRequest<Result<bool>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.StoreId).NotEmpty();
            RuleFor(x => x.Data.Name).NotEmpty();
            RuleFor(x => x.Data.Price).GreaterThan(0);
        }
    }

    public class Handler(CatalogDbContext dbContext, IEventPublisher eventPublisher)
        : IRequestHandler<Request, Result<bool>>
    {
        public async Task<Result<bool>> Handle(Request request, CancellationToken cancellationToken)
        {
            var product = await dbContext
                .Products.Include(p => p.Images)
                .Include(p => p.Attributes)
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (product is null)
            {
                return new NotFound("Product not found");
            }

            if (product.StoreId != request.StoreId)
            {
                return new ForbiddenError();
            }

            product.Name = request.Data.Name;
            product.Description = request.Data.Description;
            product.Price = request.Data.Price;
            product.Sku = request.Data.Sku;

            product.Images.Clear();
            if (request.Data.Images.Any())
            {
                product.Images = request
                    .Data.Images.Select(i => new ProductImage
                    {
                        ImageUrl = i.ImageUrl,
                        IsPrimary = i.IsPrimary,
                    })
                    .ToList();
            }

            product.Attributes.Clear();
            if (request.Data.Attributes.Any())
            {
                product.Attributes = request
                    .Data.Attributes.Select(a => new ProductAttribute
                    {
                        Key = a.Key,
                        Value = a.Value,
                    })
                    .ToList();
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            await eventPublisher.PublishAsync(
                new ProductUpdatedEvent(
                    product.Id,
                    product.Name,
                    product.Description,
                    product.Price,
                    product.Sku,
                    product.StoreId
                ),
                "Catalog.Product.ProductUpdatedEvent",
                cancellationToken
            );

            return true;
        }
    }

    public class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPut(
                    "api/products/{id:guid}",
                    async (Guid id, RequestDto data, IMediator mediator, ClaimsPrincipal user) =>
                    {
                        if (user.GetStoreId() is not { } storeId)
                        {
                            return Results.Unauthorized();
                        }

                        var result = await mediator.Send(new Request(id, storeId, data));
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization("Seller")
                .WithTags("Products");
        }
    }
}
