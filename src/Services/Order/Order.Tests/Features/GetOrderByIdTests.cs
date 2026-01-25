using Core.Domain.Errors;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Moq;
using Order.API.Constants;
using Order.API.Data;
using Order.API.Entities;
using Order.API.Features;

namespace Order.Tests.Features;

public class GetOrderByIdTests
{
    public class ValidatorTests
    {
        private readonly GetOrderById.Validator _validator = new();

        [Fact]
        public void Should_Have_Error_When_Id_Is_Empty()
        {
            // Arrange
            var request = new GetOrderById.Request(Guid.Empty);

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result
                .ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Order ID is required.");
        }

        [Fact]
        public void Should_Pass_When_Id_Is_Valid()
        {
            // Arrange
            var request = new GetOrderById.Request(Guid.NewGuid());

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }

    public class HandlerTests
    {
        private readonly OrderDbContext _dbContext;
        private readonly Mock<IValidator<GetOrderById.Request>> _validatorMock;
        private readonly GetOrderById.Handler _handler;

        public HandlerTests()
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new OrderDbContext(options);

            _validatorMock = new Mock<IValidator<GetOrderById.Request>>();
            _handler = new GetOrderById.Handler(_dbContext, _validatorMock.Object);
        }

        [Fact]
        public async Task Handle_Should_Return_ValidationError_When_Validation_Fails()
        {
            // Arrange
            _validatorMock
                .Setup(v =>
                    v.ValidateAsync(It.IsAny<GetOrderById.Request>(), It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(new ValidationResult([new ValidationFailure("Id", "Required")]));

            var request = new GetOrderById.Request(Guid.Empty);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Error.Should().BeOfType<ValidationError>();
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_Order_Does_Not_Exist()
        {
            // Arrange
            _validatorMock
                .Setup(v =>
                    v.ValidateAsync(It.IsAny<GetOrderById.Request>(), It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(new ValidationResult());

            var request = new GetOrderById.Request(Guid.NewGuid());

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Error.Should().BeOfType<NotFound>();
        }

        [Fact]
        public async Task Handle_Should_Return_Order_With_Items_When_Found()
        {
            // Arrange
            _validatorMock
                .Setup(v =>
                    v.ValidateAsync(It.IsAny<GetOrderById.Request>(), It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(new ValidationResult());

            var orderId = Guid.NewGuid();
            var storeId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var orderNumber = "ORD-12345";
            // Seed Database with Order AND Items
            var order = new Order.API.Entities.Order
            {
                Id = orderId,
                StoreId = storeId,
                UserId = userId,
                OrderNumber = orderNumber,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                Items =
                [
                    new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        ProductName = "Item 1",
                        Quantity = 1,
                        UnitPrice = 10,
                        Sku = "SKU1",
                    },
                    new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        ProductName = "Item 2",
                        Quantity = 2,
                        UnitPrice = 20,
                        Sku = "SKU2",
                    },
                ],
            };

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();

            var request = new GetOrderById.Request(orderId);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Value.Should().NotBeNull();
            result.Value.Id.Should().Be(orderId);

            // Critical: Verify 'Include(o => o.Items)' worked
            result.Value.Items.Should().HaveCount(2);
            result.Value.Items.First().ProductName.Should().Be("Item 1");
        }
    }
}
