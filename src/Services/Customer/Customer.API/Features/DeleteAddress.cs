using System.Security.Claims;
using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using Customer.API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Customer.API.Features;

public static class DeleteAddress
{
    public record Request(Guid AddressId) : IRequest<Result<bool>>;

    public class Handler(CustomerDbContext dbContext, IHttpContextAccessor httpContextAccessor)
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

            var customer = await dbContext
                .Customers.Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.IdentityId == identityId, cancellationToken);

            if (customer is null)
            {
                return new NotFound("Customer profile not found.");
            }

            var address = customer.Addresses.FirstOrDefault(a => a.Id == request.AddressId);

            if (address is null)
            {
                return new NotFound("Address not found.");
            }

            dbContext.Addresses.Remove(address);
            await dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }
    }

    public class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapDelete(
                    "api/customers/me/addresses/{id:guid}",
                    async (Guid id, IMediator mediator) =>
                    {
                        var result = await mediator.Send(new Request(id));
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags("Customers");
        }
    }
}
