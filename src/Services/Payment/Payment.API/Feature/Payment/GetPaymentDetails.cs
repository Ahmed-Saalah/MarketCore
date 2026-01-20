using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Payment.API.Data;

namespace Payment.API.Feature.Payment;

public static class GetPaymentDetails
{
    public record ResponseDto(string Status, string? ClientSecret, string? FailureMessage);

    public sealed class Response : Result<ResponseDto>
    {
        public static implicit operator Response(ResponseDto success) => new() { Value = success };

        public static implicit operator Response(DomainError error) => new() { Error = error };
    }

    public record Query(Guid OrderId) : IRequest<Response>;

    public class Handler(PaymentDbContext dbContext) : IRequestHandler<Query, Response>
    {
        private readonly PaymentDbContext _dbContext = dbContext;

        public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
        {
            var payment = await _dbContext.Payments.FirstOrDefaultAsync(
                p => p.OrderId == request.OrderId,
                cancellationToken
            );

            if (payment == null)
            {
                return new ResponseDto("Processing", null, null);
            }

            return new ResponseDto(
                payment.Status.ToString(),
                payment.StripeClientSecret,
                payment.FailureMessage
            );
        }
    }

    public class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet(
                "/api/payments/order/{orderId}",
                async (Guid orderId, IMediator mediator) =>
                {
                    var response = await mediator.Send(new Query(orderId));
                    return response.ToHttpResult();
                }
            );
        }
    }
}
