using Catalog.API.Data;
using Catalog.API.Entities.Categories;
using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using Core.Messaging;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Features.Categories;

public sealed class CreateCategory
{
    public sealed record Request(string Name, string Description, Guid? ParentId)
        : IRequest<Result<Guid>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Description).MaximumLength(500);
        }
    }

    public sealed class Handler(CatalogDbContext dbContext, IEventPublisher eventPublisher)
        : IRequestHandler<Request, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(Request request, CancellationToken cancellationToken)
        {
            if (request.ParentId.HasValue)
            {
                var parentExists = await dbContext.Categories.AnyAsync(
                    c => c.Id == request.ParentId,
                    cancellationToken
                );

                if (!parentExists)
                {
                    return new NotFound("Parent Category not found.");
                }
            }

            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                ParentId = request.ParentId,
                CreatedAt = DateTime.UtcNow,
            };

            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync(cancellationToken);

            await eventPublisher.PublishAsync(
                new Messages.Categories.CategoryCreatedEvent(
                    category.Id,
                    category.Name,
                    category.Description,
                    category.ImageUrl,
                    category.IsActive,
                    category.ParentId,
                    category.CreatedAt
                ),
                routingKey: "Catalog.CategoryCreatedEvent",
                cancellationToken
            );

            return category.Id;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "api/categories",
                    async (Request request, IMediator mediator) =>
                    {
                        var result = await mediator.Send(request);
                        return result.ToHttpResult();
                    }
                )
                .WithTags("Categories");
        }
    }
}
