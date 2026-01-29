using Catalog.API.Data;
using Catalog.API.Messages.Categories;
using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using Core.Messaging;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Features.Categories;

public sealed class UpdateCategory
{
    public sealed record Request(Guid Id, string Name, string Description, Guid? ParentId)
        : IRequest<Result<bool>>;

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Description).MaximumLength(500);

            RuleFor(x => x)
                .Must(x => x.ParentId != x.Id)
                .WithMessage("A category cannot be its own parent.");
        }
    }

    public sealed class Handler(CatalogDbContext dbContext, IEventPublisher eventPublisher)
        : IRequestHandler<Request, Result<bool>>
    {
        public async Task<Result<bool>> Handle(Request request, CancellationToken cancellationToken)
        {
            var category = await dbContext.Categories.FirstOrDefaultAsync(
                c => c.Id == request.Id,
                cancellationToken
            );

            if (category is null)
            {
                return new NotFound("Category not found.");
            }

            if (request.ParentId.HasValue && request.ParentId != category.ParentId)
            {
                var parentExists = await dbContext.Categories.AnyAsync(
                    c => c.Id == request.ParentId,
                    cancellationToken
                );

                if (!parentExists)
                {
                    return new NotFound("Parent category does not exist.");
                }
                // TODO: Check for circular references in category hierarchy
            }

            category.Name = request.Name;
            category.Description = request.Description;
            category.ParentId = request.ParentId;

            await dbContext.SaveChangesAsync(cancellationToken);

            await eventPublisher.PublishAsync(
                new CategoryUpdatedEvent(
                    category.Id,
                    category.Name,
                    category.Description,
                    category.ImageUrl,
                    category.IsActive,
                    category.ParentId,
                    DateTime.UtcNow
                ),
                cancellationToken
            );
            return true;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPut(
                    "api/categories/{id:guid}",
                    async (Guid id, Request request, IMediator mediator) =>
                    {
                        if (id != request.Id)
                            return Results.BadRequest("ID mismatch");

                        var result = await mediator.Send(request);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization("Admin")
                .WithTags("Categories");
        }
    }
}
