using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.API.Data;

namespace Order.API.Features;

public sealed class GetOrderByOrderNumber
{
    public sealed record Request(string OrderNumber) : IRequest<Response>;

    public sealed class Response : Result<Entities.Order>
    {
        public static implicit operator Response(Entities.Order order) => new() { Value = order };

        public static implicit operator Response(DomainError error) => new() { Error = error };
    }

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(r => r.OrderNumber).NotEmpty().WithMessage("Order number required.");
        }
    }

    public sealed class Handler(OrderDbContext dbContext, IValidator<Request> validator)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return new ValidationError(validationResult.Errors);
            }

            var order = await dbContext
                .Orders.Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.OrderNumber == request.OrderNumber, cancellationToken);

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
                "api/orders/{orderNumer}",
                async (IMediator mediator, string orderNumer) =>
                {
                    var response = await mediator.Send(new Request(orderNumer));
                    return response.ToHttpResult();
                }
            );
        }
    }
}
