using System.Security.Claims;
using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Store.API.Data;

namespace Store.API.Features;

public sealed class UpdateStore
{
    public record Request(string Name, string Description, string? LogoUrl, string? CoverImageUrl)
        : IRequest<Result<bool>>;

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        }
    }

    public class Handler(StoreDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        : IRequestHandler<Request, Result<bool>>
    {
        public async Task<Result<bool>> Handle(Request request, CancellationToken cancellationToken)
        {
            var user = httpContextAccessor.HttpContext?.User;
            var identityIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (
                string.IsNullOrEmpty(identityIdClaim)
                || !int.TryParse(identityIdClaim, out int identityId)
            )
            {
                return new UnauthorizedError("User identity could not be verified.");
            }

            var store = await dbContext.Stores.FirstOrDefaultAsync(
                s => s.OwnerIdentityId == identityId,
                cancellationToken
            );

            if (store is null)
            {
                return new NotFound("Store profile not found.");
            }

            store.Name = request.Name;
            store.Description = request.Description;
            store.LogoUrl = request.LogoUrl;
            store.CoverImageUrl = request.CoverImageUrl;
            store.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }
    }

    public class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPut(
                    "api/stores/me",
                    async (Request request, IMediator mediator) =>
                    {
                        var result = await mediator.Send(request);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags("Stores");
        }
    }
}
