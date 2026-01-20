using Microsoft.Extensions.Options;
using Payment.API.Configuration;
using Stripe;

namespace Payment.API.Services;

public class StripePaymentGateway : IPaymentGateway
{
    private readonly StripeOptions _options;

    public StripePaymentGateway(IOptions<StripeOptions> options)
    {
        _options = options.Value;
        StripeConfiguration.ApiKey = _options.SecretKey;
    }

    public async Task<PaymentResult> CreatePaymentIntentAsync(
        decimal amount,
        string currency,
        Guid orderId
    )
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100),
                Currency = currency,
                PaymentMethodTypes = new List<string> { "card" },
                Metadata = new Dictionary<string, string> { { "OrderId", orderId.ToString() } },
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            return new PaymentResult(true, paymentIntent.Id, paymentIntent.ClientSecret, null);
        }
        catch (StripeException ex)
        {
            return new PaymentResult(false, null, null, ex.Message);
        }
    }
}
