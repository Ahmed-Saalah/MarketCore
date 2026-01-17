using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using Customer.API.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Customer.API.Features;

public sealed class GetCustomerById
{
    public record Response(
        Guid Id,
        string UserName,
        string Email,
        string DisplayName,
        string PhoneNumber,
        List<AddressDto> Addresses
    );

    public record AddressDto(
        Guid Id,
        string Street,
        string City,
        string State,
        string Country,
        string ZipCode,
        bool IsDefault
    );

    public record Request(Guid Id) : IRequest<Result<Response>>;

    public class Handler(CustomerDbContext dbContext) : IRequestHandler<Request, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Request request,
            CancellationToken cancellationToken
        )
        {
            var customer = await dbContext
                .Customers.AsNoTracking()
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (customer is null)
            {
                return new NotFound("Customer not found.");
            }

            var response = new Response(
                customer.Id,
                customer.UserName,
                customer.Email,
                customer.DisplayName,
                customer.PhoneNumber ?? string.Empty,
                customer
                    .Addresses.Select(a => new AddressDto(
                        a.Id,
                        a.Street,
                        a.City,
                        a.State,
                        a.Country,
                        a.ZipCode,
                        a.IsDefault
                    ))
                    .ToList()
            );

            return response;
        }
    }

    public class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "api/customers/{id:guid}",
                    async (Guid id, IMediator mediator) =>
                    {
                        var result = await mediator.Send(new Request(id));
                        return result.ToHttpResult();
                    }
                )
                .WithTags("Customers");
        }
    }
}
