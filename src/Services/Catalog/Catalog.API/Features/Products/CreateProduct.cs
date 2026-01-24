using System.Security.Claims;
using Catalog.API.Data;
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

public sealed class CreateProduct
{
    public sealed record ImageDto(string ImageUrl, bool IsPrimary);

    public sealed record AttributeDto(string Key, string Value);

    public sealed record RequestDto(
        Guid CategoryId,
        string Name,
        string Description,
        string Sku,
        decimal Price,
        string Currency,
        List<ImageDto> Images,
        List<AttributeDto> Attributes
    );

    public sealed record Request(Guid StoreId, RequestDto Data) : IRequest<Result<Guid>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Data.Name).NotEmpty();
            RuleFor(x => x.Data.Sku).NotEmpty().MinimumLength(3);
            RuleFor(x => x.Data.Price).GreaterThan(0);
            RuleFor(x => x.Data.CategoryId).NotEmpty();
            RuleFor(x => x.StoreId).NotEmpty();
        }
    }

    public class Handler(
        CatalogDbContext dbContext,
        IValidator<Request> validator,
        IEventPublisher eventPublisher
    ) : IRequestHandler<Request, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(Request request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return new ValidationError(validationResult.Errors);
            }

            var skuExists = await dbContext.Products.AnyAsync(
                p => p.Sku == request.Data.Sku,
                cancellationToken
            );

            if (skuExists)
            {
                return new ConflictError("This SKU is already in use.");
            }

            var category = await dbContext.Categories.FirstOrDefaultAsync(
                c => c.Id == request.Data.CategoryId,
                cancellationToken
            );

            if (category is null)
            {
                return new NotFound("Category not found.");
            }

            var product = new Product
            {
                Id = Guid.NewGuid(),
                StoreId = request.StoreId,
                CategoryId = request.Data.CategoryId,
                Name = request.Data.Name,
                Description = request.Data.Description,
                Sku = request.Data.Sku,
                Price = request.Data.Price,
                Currency = request.Data.Currency,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

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

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync(cancellationToken);

            await eventPublisher.PublishAsync(
                new ProductCreatedEvent(
                    product.Id,
                    product.StoreId,
                    product.Sku,
                    product.Name,
                    product.Description,
                    product.Price,
                    product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? string.Empty,
                    category.Id,
                    category.Name,
                    product.CreatedAt
                ),
                "Catalog.ProductCreatedEvent",
                cancellationToken
            );

            return product.Id;
        }
    }

    public class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "api/products",
                    async (RequestDto data, IMediator mediator, ClaimsPrincipal user) =>
                    {
                        if (user.GetStoreId() is not { } storeId)
                        {
                            return Results.Unauthorized();
                        }
                        var result = await mediator.Send(new Request(storeId, data));
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization("Seller")
                .WithTags("Products");
        }
    }
}
