using Core.Domain.Errors;
using Core.Messaging;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Moq;
using Order.API.Constants;
using Order.API.Data;
using Order.API.Features;
using Order.API.Messages;

namespace Order.Tests.Features;

public class CreateOrderTests
{
    public class ValidatorTests
    {
        private readonly CreateOrder.Validator _validator = new();

        [Fact]
        public void Should_Have_Error_When_StoreId_Is_Empty()
        {
            var request = CreateRequest(storeId: Guid.Empty);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.StoreId);
        }

        [Fact]
        public void Should_Have_Error_When_UserId_Is_Empty()
        {
            var request = CreateRequest(userId: Guid.Empty);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.UserId);
        }

        [Fact]
        public void Should_Have_Error_When_Items_List_Is_Empty()
        {
            var request = new CreateOrder.Request(Guid.NewGuid(), Guid.NewGuid(), []);
            var result = _validator.TestValidate(request);
            result
                .ShouldHaveValidationErrorFor(x => x.Items)
                .WithErrorMessage("Order must contain at least one item.");
        }

        [Fact]
        public void Should_Have_Error_When_Item_ProductId_Is_Empty()
        {
            var items = new List<CreateOrder.CreateOrderItemDto>
            {
                new(Guid.Empty, "Product A", "SKU-A", 10m, 1),
            };
            var request = new CreateOrder.Request(Guid.NewGuid(), Guid.NewGuid(), items);

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor("Items[0].ProductId");
        }

        [Fact]
        public void Should_Have_Error_When_Item_Quantity_Is_Zero_Or_Negative()
        {
            var items = new List<CreateOrder.CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Product A", "SKU-A", 10m, 0),
            };
            var request = new CreateOrder.Request(Guid.NewGuid(), Guid.NewGuid(), items);

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor("Items[0].Quantity");
        }

        [Fact]
        public void Should_Have_Error_When_Item_UnitPrice_Is_Negative()
        {
            var items = new List<CreateOrder.CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Product A", "SKU-A", -5m, 1),
            };
            var request = new CreateOrder.Request(Guid.NewGuid(), Guid.NewGuid(), items);

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor("Items[0].UnitPrice");
        }

        [Fact]
        public void Should_Pass_When_Request_Is_Valid()
        {
            var request = CreateRequest();
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveAnyValidationErrors();
        }

        private static CreateOrder.Request CreateRequest(Guid? userId = null, Guid? storeId = null)
        {
            return new CreateOrder.Request(
                userId ?? Guid.NewGuid(),
                storeId ?? Guid.NewGuid(),
                [new(Guid.NewGuid(), "Test Product", "SKU-123", 100m, 1)]
            );
        }
    }

    public class HandlerTests
    {
        private readonly OrderDbContext _dbContext;
        private readonly Mock<IValidator<CreateOrder.Request>> _validatorMock;
        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly CreateOrder.Handler _handler;

        public HandlerTests()
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new OrderDbContext(options);

            _validatorMock = new Mock<IValidator<CreateOrder.Request>>();
            _eventPublisherMock = new Mock<IEventPublisher>();

            _validatorMock
                .Setup(v =>
                    v.ValidateAsync(It.IsAny<CreateOrder.Request>(), It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(new ValidationResult());

            _handler = new CreateOrder.Handler(
                _dbContext,
                _validatorMock.Object,
                _eventPublisherMock.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Return_ValidationError_When_Validation_Fails()
        {
            _validatorMock
                .Setup(v =>
                    v.ValidateAsync(It.IsAny<CreateOrder.Request>(), It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(new ValidationResult([new ValidationFailure("StoreId", "Required")]));

            var request = CreateValidRequest();

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Error.Should().BeOfType<ValidationError>();
            (await _dbContext.Orders.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task Handle_Should_Create_Order_With_Correct_Totals()
        {
            // Arrange
            var item1 = new CreateOrder.CreateOrderItemDto(Guid.NewGuid(), "P1", "SKU1", 50m, 2); // 50 * 2 = 100
            var item2 = new CreateOrder.CreateOrderItemDto(Guid.NewGuid(), "P2", "SKU2", 20m, 1); // 20 * 1 = 20
            // Expected Subtotal: 120

            var request = new CreateOrder.Request(Guid.NewGuid(), Guid.NewGuid(), [item1, item2]);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert

            var savedOrder = await _dbContext
                .Orders.Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == result.Value.OrderId);

            savedOrder.Should().NotBeNull();
            savedOrder!.StoreId.Should().Be(request.StoreId);
            savedOrder.UserId.Should().Be(request.UserId);
            savedOrder.Status.Should().Be(OrderStatus.Pending);

            savedOrder.Subtotal.Should().Be(120m);
            savedOrder.Total.Should().Be(120m);
            savedOrder.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task Handle_Should_Publish_OrderCreatedEvent()
        {
            // Arrange
            var request = CreateValidRequest();

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            _eventPublisherMock.Verify(
                x =>
                    x.PublishAsync(
                        It.Is<OrderCreatedEvent>(e =>
                            e.OrderId == result.Value.OrderId
                            && e.StoreId == request.StoreId
                            && e.UserId == request.UserId
                            && e.Items.Count == request.Items.Count
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        private static CreateOrder.Request CreateValidRequest()
        {
            return new CreateOrder.Request(
                Guid.NewGuid(),
                Guid.NewGuid(),
                [new(Guid.NewGuid(), "Product A", "SKU-A", 100m, 1)]
            );
        }
    }
}
