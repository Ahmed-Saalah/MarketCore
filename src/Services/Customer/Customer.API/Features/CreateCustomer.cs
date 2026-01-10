using Customer.API.Data;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Customer.API.Features;

public sealed class CreateCustomer
{
    public record Command(
        int IdentityId,
        string Username,
        string Email,
        string DisplayName,
        string? PhoneNumber
    ) : IRequest;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.IdentityId).GreaterThan(0);
            RuleFor(x => x.Email).NotEmpty();
            RuleFor(x => x.Username).NotEmpty();
        }
    }

    public class Handler(CustomerDbContext dbContext) : IRequestHandler<Command>
    {
        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            var exists = await dbContext.Customers.AnyAsync(
                c => c.IdentityId == request.IdentityId,
                cancellationToken
            );

            if (exists)
            {
                return;
            }

            var newCustomer = new Entities.Customer
            {
                Id = Guid.NewGuid(),
                IdentityId = request.IdentityId,
                UserName = request.Username,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                DisplayName = request.DisplayName,
                CreatedAt = DateTime.UtcNow,
            };

            dbContext.Customers.Add(newCustomer);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
