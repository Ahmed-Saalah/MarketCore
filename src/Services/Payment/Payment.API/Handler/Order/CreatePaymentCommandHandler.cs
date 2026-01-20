using Core.Messaging;
using Microsoft.EntityFrameworkCore;
using Payment.API.Constants;
using Payment.API.Data;
using Payment.API.Services;

namespace Payment.API.Handler.Order;

public sealed class CreatePaymentCommandHandler
{
    public record Command(Guid OrderId, Guid UserId, decimal Amount, string Currency);

    public sealed class Handler(
        PaymentDbContext dbContext,
        IPaymentGateway paymentGateway,
        ILogger<Handler> logger
    ) : IEventHandler<Command>
    {
        private readonly PaymentDbContext _dbContext = dbContext;
        private readonly IPaymentGateway _paymentGateway = paymentGateway;
        private readonly ILogger<Handler> _logger = logger;

        public async Task HandleAsync(
            Command command,
            CancellationToken cancellationToken = default
        )
        {
            _logger.LogInformation(
                "Initiating Payment Intent for Order {OrderId}",
                command.OrderId
            );

            var existingPayment = await _dbContext.Payments.FirstOrDefaultAsync(
                p => p.OrderId == command.OrderId,
                cancellationToken
            );
            if (existingPayment != null)
            {
                if (existingPayment.Status == PaymentStatus.Succeeded)
                {
                    _logger.LogInformation(
                        "Payment for Order {OrderId} has already succeeded. Skipping.",
                        command.OrderId
                    );
                    return;
                }

                if (!string.IsNullOrEmpty(existingPayment.StripePaymentIntentId))
                {
                    _logger.LogInformation(
                        "Payment Intent already exists for Order {OrderId}. Skipping creation.",
                        command.OrderId
                    );
                    return;
                }
            }
            var payment = new Entities.Payment
            {
                Id = Guid.NewGuid(),
                OrderId = command.OrderId,
                UserId = command.UserId,
                Amount = command.Amount,
                Currency = command.Currency,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
            };

            _dbContext.Payments.Add(payment);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var result = await _paymentGateway.CreatePaymentIntentAsync(
                command.Amount,
                command.Currency,
                command.OrderId
            );

            if (result.IsSuccess)
            {
                payment.StripePaymentIntentId = result.PaymentIntentId;
                payment.StripeClientSecret = result.ClientSecret;
                payment.Status = PaymentStatus.RequiresConfirmation;

                _logger.LogInformation(
                    "Stripe Payment Intent created: {PaymentIntentId}",
                    result.PaymentIntentId
                );
            }
            else
            {
                payment.Status = PaymentStatus.Failed;
                payment.FailureMessage = result.ErrorMessage;
                _logger.LogError("Stripe Intent Creation Failed: {Error}", result.ErrorMessage);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
