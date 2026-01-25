using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Payment.API.Constants;
using Payment.API.Data;
using Payment.API.Handler.Order;
using Payment.API.Services;

namespace Payment.Tests.Handlers;

public class CreatePaymentCommandHandlerTests
{
    private readonly PaymentDbContext _dbContext;
    private readonly Mock<IPaymentGateway> _paymentGatewayMock;
    private readonly Mock<ILogger<CreatePaymentCommandHandler.Handler>> _loggerMock;
    private readonly CreatePaymentCommandHandler.Handler _handler;

    public CreatePaymentCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new PaymentDbContext(options);
        _paymentGatewayMock = new Mock<IPaymentGateway>();
        _loggerMock = new Mock<ILogger<CreatePaymentCommandHandler.Handler>>();
        _handler = new CreatePaymentCommandHandler.Handler(
            _dbContext,
            _paymentGatewayMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Handle_Should_DoNothing_When_Payment_Already_Succeeded()
    {
        // Arrange
        var command = CreateCommand();
        _dbContext.Payments.Add(
            new API.Entities.Payment
            {
                Id = Guid.NewGuid(),
                OrderId = command.OrderId,
                Status = PaymentStatus.Succeeded,
                StripePaymentIntentId = "pi_existing",
            }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _paymentGatewayMock.Verify(
            x =>
                x.CreatePaymentIntentAsync(
                    It.IsAny<decimal>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_Should_DoNothing_When_PaymentIntent_Already_Exists()
    {
        // Arrange
        var command = CreateCommand();

        _dbContext.Payments.Add(
            new API.Entities.Payment
            {
                Id = Guid.NewGuid(),
                OrderId = command.OrderId,
                Status = PaymentStatus.Pending,
                StripePaymentIntentId = "pi_existing_intent",
            }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _paymentGatewayMock.Verify(
            x =>
                x.CreatePaymentIntentAsync(
                    It.IsAny<decimal>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_Should_Create_Payment_And_Intent_When_New_Order()
    {
        // Arrange
        var command = CreateCommand();
        var expectedIntentId = "pi_new_123";
        var expectedSecret = "secret_123";

        _paymentGatewayMock
            .Setup(x =>
                x.CreatePaymentIntentAsync(command.Amount, command.Currency, command.OrderId)
            )
            .ReturnsAsync(
                new PaymentResult(
                    IsSuccess: true,
                    PaymentIntentId: expectedIntentId,
                    ClientSecret: expectedSecret,
                    ErrorMessage: null
                )
            );

        // Act
        await _handler.HandleAsync(command);

        // Assert
        var payment = await _dbContext.Payments.FirstOrDefaultAsync(p =>
            p.OrderId == command.OrderId
        );

        payment.Should().NotBeNull();
        payment!.Status.Should().Be(PaymentStatus.RequiresConfirmation);
        payment.StripePaymentIntentId.Should().Be(expectedIntentId);
        payment.StripeClientSecret.Should().Be(expectedSecret);
        payment.Amount.Should().Be(command.Amount);
    }

    [Fact]
    public async Task Handle_Should_Set_Failed_Status_When_Gateway_Fails()
    {
        // Arrange
        var command = CreateCommand();
        var errorMessage = "Stripe API Error";

        // Mock Gateway Failure
        _paymentGatewayMock
            .Setup(x =>
                x.CreatePaymentIntentAsync(command.Amount, command.Currency, command.OrderId)
            )
            .ReturnsAsync(
                new PaymentResult(
                    IsSuccess: false,
                    PaymentIntentId: null,
                    ClientSecret: null,
                    ErrorMessage: errorMessage
                )
            );

        // Act
        await _handler.HandleAsync(command);

        // Assert
        var payment = await _dbContext.Payments.FirstOrDefaultAsync(p =>
            p.OrderId == command.OrderId
        );

        payment.Should().NotBeNull();
        payment!.Status.Should().Be(PaymentStatus.Failed);
        payment.FailureMessage.Should().Be(errorMessage);
        payment.StripePaymentIntentId.Should().BeNull();
    }

    private static CreatePaymentCommandHandler.Command CreateCommand()
    {
        return new CreatePaymentCommandHandler.Command(
            OrderId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            Amount: 100m,
            Currency: "USD"
        );
    }
}
