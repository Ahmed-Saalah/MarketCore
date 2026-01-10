using System.Security.Claims;
using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Customer.API.Data;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Customer.API.Features;

public static class UpdateCustomer
{
    public record Request(string DisplayName, string? PhoneNumber) : IRequest<Result<bool>>;

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.PhoneNumber).MaximumLength(20);
        }
    }

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

            var customer = await dbContext.Customers.FirstOrDefaultAsync(
                c => c.IdentityId == identityId,
                cancellationToken
            );

            if (customer is null)
            {
                return new NotFound("Customer profile not found.");
            }

            customer.DisplayName = request.DisplayName;
            customer.PhoneNumber = request.PhoneNumber;
            customer.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }
    }

    public class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPut(
                    "api/customers/me",
                    async (Request request, IMediator mediator) =>
                    {
                        var result = await mediator.Send(request);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags("Customers");
        }
    }
}
