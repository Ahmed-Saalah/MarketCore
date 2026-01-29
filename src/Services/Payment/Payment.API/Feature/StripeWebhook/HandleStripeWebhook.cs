using Core.Domain.Abstractions;
using Core.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Payment.API.Configuration;
using Payment.API.Constants;
using Payment.API.Data;
using Payment.API.Messages;
using Stripe;

namespace Payment.API.Feature.StripeWebhook;

public sealed class HandleStripeWebhook
{
    public record Command(string Json, string Signature) : IRequest<IResult>;

    public sealed class Handler(
        PaymentDbContext dbContext,
        IOptions<StripeOptions> options,
        IEventPublisher eventPublisher,
        ILogger<HandleStripeWebhook.Handler> logger
    ) : IRequestHandler<Command, IResult>
    {
        private readonly PaymentDbContext _dbContext = dbContext;
        private readonly StripeOptions _options = options.Value;
        private readonly IEventPublisher _eventPublisher = eventPublisher;
        private readonly ILogger<Handler> _logger = logger;

        public async Task<IResult> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    request.Json,
                    request.Signature,
                    _options.WebhookSecret
                );

                if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
                {
                    if (stripeEvent.Data.Object is PaymentIntent intent)
                    {
                        await HandleSuccess(intent, cancellationToken);
                    }
                }
                else if (stripeEvent.Type == EventTypes.PaymentIntentPaymentFailed)
                {
                    if (stripeEvent.Data.Object is PaymentIntent intent)
                    {
                        await HandleFailure(intent, cancellationToken);
                    }
                }

                return Results.Ok();
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe Signature Verification Failed");
                return Results.BadRequest("Invalid Stripe Signature");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe Webhook");
                return Results.StatusCode(500);
            }
        }

        private async Task HandleSuccess(PaymentIntent intent, CancellationToken ct)
        {
            var payment = await _dbContext.Payments.FirstOrDefaultAsync(
                p => p.StripePaymentIntentId == intent.Id,
                ct
            );

            if (payment == null)
                return;

            payment.Status = PaymentStatus.Succeeded;
            payment.CompletedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Payment Succeeded for Order {OrderId}", payment.OrderId);

            await _eventPublisher.PublishAsync(
                new PaymentSucceededEvent(payment.OrderId, payment.Id, payment.Amount, intent.Id),
                ct
            );
        }

        private async Task HandleFailure(PaymentIntent intent, CancellationToken ct)
        {
            var payment = await _dbContext.Payments.FirstOrDefaultAsync(
                p => p.StripePaymentIntentId == intent.Id,
                ct
            );

            if (payment == null)
                return;

            payment.Status = PaymentStatus.Failed;
            payment.FailureMessage = intent.LastPaymentError?.Message ?? "Unknown Error";

            await _dbContext.SaveChangesAsync(ct);
            _logger.LogWarning("Payment Failed for Order {OrderId}", payment.OrderId);

            await _eventPublisher.PublishAsync(
                new PaymentFailedEvent(payment.OrderId, payment.FailureMessage),
                ct
            );
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "/api/webhooks",
                    async (HttpContext context, IMediator mediator) =>
                    {
                        using var reader = new StreamReader(context.Request.Body);
                        var json = await reader.ReadToEndAsync();
                        var signature = context.Request.Headers["Stripe-Signature"].ToString();
                        return await mediator.Send(new Command(json, signature));
                    }
                )
                .AllowAnonymous();
        }
    }
}
