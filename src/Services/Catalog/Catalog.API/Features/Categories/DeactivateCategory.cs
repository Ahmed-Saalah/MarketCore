using Catalog.API.Data;
using Catalog.API.Messages.Categories;
using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Features.Categories;

public static class DeactivateCategory
{
    public record Request(Guid Id) : IRequest<Result<bool>>;

    public class Handler(CatalogDbContext dbContext, IEventPublisher eventPublisher)
        : IRequestHandler<Request, Result<bool>>
    {
        public async Task<Result<bool>> Handle(Request request, CancellationToken cancellationToken)
        {
            var targetCategory = await dbContext.Categories.FirstOrDefaultAsync(
                c => c.Id == request.Id,
                cancellationToken
            );

            if (targetCategory is null)
            {
                return new NotFound("Category not found.");
            }

            var allCategories = await dbContext
                .Categories.Select(c => new { c.Id, c.ParentId })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var idsToDeactivate = new HashSet<Guid> { targetCategory.Id };
            CollectChildrenIds(targetCategory.Id, allCategories, idsToDeactivate);

            await dbContext
                .Categories.Where(c => idsToDeactivate.Contains(c.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, false), cancellationToken);

            await eventPublisher.PublishAsync(
                new CategoryDeactivatedEvent(
                    RootCategoryId: request.Id,
                    AllDeactivatedCategoryIds: idsToDeactivate.ToList()
                ),
                routingKey: "catalog.category.deactivated",
                cancellationToken
            );

            return true;
        }

        private void CollectChildrenIds(
            Guid parentId,
            IEnumerable<dynamic> allCategories,
            HashSet<Guid> results
        )
        {
            var children = allCategories.Where(c => c.ParentId == parentId).Select(c => c.Id);

            foreach (var childId in children)
            {
                if (results.Add(childId))
                {
                    CollectChildrenIds(childId, allCategories, results);
                }
            }
        }
    }

    public class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPatch(
                    "api/categories/{id:guid}/deactivate",
                    async (Guid id, IMediator mediator) =>
                    {
                        var result = await mediator.Send(new Request(id));
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization("Admin")
                .WithTags("Categories");
        }
    }
}
