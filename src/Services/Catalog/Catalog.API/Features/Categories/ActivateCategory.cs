using Catalog.API.Data;
using Catalog.API.Messages.Categories;
using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using Core.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Features.Categories;

public static class ActivateCategory
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

            var idsToAactivate = new HashSet<Guid> { targetCategory.Id };
            CollectChildrenIds(targetCategory.Id, allCategories, idsToAactivate);

            await dbContext
                .Categories.Where(c => idsToAactivate.Contains(c.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, true), cancellationToken);

            await eventPublisher.PublishAsync(
                new CategoryActivatedEvent(
                    RootCategoryId: request.Id,
                    AllActivatedCategoryIds: idsToAactivate.ToList()
                ),
                routingKey: "catalog.category.Activated",
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
                    "api/categories/{id:guid}/Activate",
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
