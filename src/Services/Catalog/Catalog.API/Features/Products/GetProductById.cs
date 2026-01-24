using System.Security.Claims;
using Catalog.API.Data;
using Catalog.API.Entities.Products;
using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Features.Products;

public sealed class GetProductById
{
    public sealed record Request(Guid Id) : IRequest<Response>;

    public sealed class Response : Result<Product>
    {
        public static implicit operator Response(Product success) => new() { Value = success };

        public static implicit operator Response(DomainError error) => new() { Error = error };
    }

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(r => r.Id).NotEmpty().WithMessage("Product Id must be provided.");
        }
    }

    public sealed class Handler(CatalogDbContext context, IValidator<Request> validator)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return new ValidationError(validationResult.Errors);
            }

            var product = await context
                .Products.Include(p => p.Images)
                .Include(p => p.Attributes)
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            return product is not null ? product : new NotFound("Product not found");
        }
    }

    public class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "api/products/{id:guid}",
                    async (Guid id, IMediator mediator) =>
                    {
                        var result = await mediator.Send(new Request(id));
                        return result.ToHttpResult();
                    }
                )
                .WithTags("Products");
        }
    }
}
