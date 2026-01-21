using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.API.Data;

namespace Order.API.Features;

public sealed class GetOrderById
{
    public sealed record Request(Guid Id) : IRequest<Response>;

    public sealed class Response : Result<Entities.Order>
    {
        public static implicit operator Response(Entities.Order order) => new() { Value = order };

        public static implicit operator Response(DomainError error) => new() { Error = error };
    }

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(r => r.Id).NotEmpty().WithMessage("Order ID is required.");
        }
    }

    public sealed class Handler(OrderDbContext dbContext) : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var order = await dbContext
                .Orders.Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

            if (order is null)
            {
                return new NotFound();
            }

            return order;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet(
                "api/orders/{id}",
                async (IMediator mediator, Guid id) =>
                {
                    var response = await mediator.Send(new Request(id));
                    return response.ToHttpResult();
                }
            );
        }
    }
}
