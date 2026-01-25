using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Payment.API.Constants;
using Payment.API.Data;
using Payment.API.Feature.Payment;

namespace Payment.Tests.Features;

public class GetPaymentDetailsTests
{
    private readonly PaymentDbContext _dbContext;
    private readonly GetPaymentDetails.Handler _handler;

    public GetPaymentDetailsTests()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new PaymentDbContext(options);
        _handler = new GetPaymentDetails.Handler(_dbContext);
    }

    [Fact]
    public async Task Handle_Should_Return_Processing_When_Payment_Not_Found()
    {
        // Arrange
        var query = new GetPaymentDetails.Query(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Status.Should().Be("Processing");
        result.Value.ClientSecret.Should().BeNull();
        result.Value.FailureMessage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Return_Details_When_Payment_Exists()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var clientSecret = "pi_12345_secret_67890";

        var payment = new API.Entities.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Status = PaymentStatus.Completed,
            StripeClientSecret = clientSecret,
            FailureMessage = null,
            Amount = 100,
            Currency = "USD",
            CreatedAt = DateTime.UtcNow,
        };

        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync();

        var query = new GetPaymentDetails.Query(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Status.Should().Be("Completed");
        result.Value.ClientSecret.Should().Be(clientSecret);
        result.Value.FailureMessage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Return_FailureMessage_When_Payment_Failed()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var errorMsg = "Insufficient funds";

        var payment = new API.Entities.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Status = PaymentStatus.Failed,
            StripeClientSecret = null,
            FailureMessage = errorMsg,
            Amount = 50,
            Currency = "USD",
        };

        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync();

        var query = new GetPaymentDetails.Query(orderId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Status.Should().Be("Failed");
        result.Value.FailureMessage.Should().Be(errorMsg);
    }
}
