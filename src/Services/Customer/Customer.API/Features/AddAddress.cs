using System.Security.Claims;
using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using Customer.API.Data;
using Customer.API.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Customer.API.Features;

public static class AddAddress
{
    public record Request(
        string Street,
        string City,
        string State,
        string Country,
        string ZipCode,
        bool IsDefault
    ) : IRequest<Result<Guid>>;

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
            RuleFor(x => x.City).NotEmpty().MaximumLength(100);
            RuleFor(x => x.State).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ZipCode).NotEmpty().MaximumLength(20);
        }
    }

    public class Handler(CustomerDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        : IRequestHandler<Request, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(Request request, CancellationToken cancellationToken)
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

            if (request.IsDefault && customer.Addresses.Any())
            {
                foreach (var address in customer.Addresses)
                {
                    address.IsDefault = false;
                }
            }

            var newAddress = new Address
            {
                Id = Guid.NewGuid(),
                Street = request.Street,
                City = request.City,
                State = request.State,
                Country = request.Country,
                ZipCode = request.ZipCode,
                IsDefault = request.IsDefault || !customer.Addresses.Any(),
                CustomerId = customer.Id,
            };

            dbContext.Addresses.Add(newAddress);
            await dbContext.SaveChangesAsync(cancellationToken);

            return newAddress.Id;
        }
    }

    public class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "api/customers/me/addresses",
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
