using Cart.API.Data;
using Cart.API.Entities;
using Cart.API.Features;
using Cart.API.Messages;
using Core.Domain.Errors;
using Core.Messaging;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using FluentValidation.TestHelper;
using Moq;

namespace Cart.Tests.Features;

public class UpdateCartItemTests
{
    public class ValidatorTests
    {
        private readonly UpdateCartItem.Validator _validator = new();

        [Fact]
        public void Should_Have_Error_When_CartId_Is_Empty()
        {
            var model = CreateRequest(cartId: Guid.Empty);
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.CartId);
        }

        [Fact]
        public void Should_Have_Error_When_ProductId_Is_Empty()
        {
            var model = CreateRequest(productId: Guid.Empty);
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Dto.ProductId);
        }

        [Fact]
        public void Should_Have_Error_When_Quantity_Is_Zero_Or_Less()
        {
            var model = CreateRequest(quantity: 0);
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Dto.Quantity);
        }

        [Fact]
        public void Should_Not_Have_Error_When_Request_Is_Valid()
        {
            var model = CreateRequest();
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveAnyValidationErrors();
        }

        private static UpdateCartItem.Request CreateRequest(
            Guid? cartId = null,
            Guid? productId = null,
            int quantity = 5
        )
        {
            return new UpdateCartItem.Request(
                CartId: cartId ?? Guid.NewGuid(),
                UserId: Guid.NewGuid(),
                Dto: new UpdateCartItem.RequestDto(
                    ProductId: productId ?? Guid.NewGuid(),
                    Quantity: quantity
                )
            );
        }
    }

    public class HandlerTests
    {
        private readonly Mock<ICartRepository> _repositoryMock;
        private readonly Mock<IValidator<UpdateCartItem.Request>> _validatorMock;
        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly UpdateCartItem.Handler _handler;

        public HandlerTests()
        {
            _repositoryMock = new Mock<ICartRepository>();
            _validatorMock = new Mock<IValidator<UpdateCartItem.Request>>();
            _eventPublisherMock = new Mock<IEventPublisher>();
            _handler = new UpdateCartItem.Handler(
                _repositoryMock.Object,
                _validatorMock.Object,
                _eventPublisherMock.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Return_ValidationError_When_Validation_Fails()
        {
            // Arrange
            var request = CreateValidRequest();
            var failure = new ValidationFailure("Prop", "Error");

            _validatorMock
                .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[] { failure }));

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Error.Should().BeOfType<ValidationError>();
            _repositoryMock.Verify(
                r => r.GetCartAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_Cart_Does_Not_Exist()
        {
            // Arrange
            var request = CreateValidRequest();
            SetupValidationSuccess(request);

            _repositoryMock
                .Setup(r => r.GetCartAsync(request.CartId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((API.Entities.Cart?)null);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Error.Should().BeOfType<NotFound>();
            result.Error.Message.Should().Be("Cart not found");
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_Item_Does_Not_Exist_In_Cart()
        {
            // Arrange
            var request = CreateValidRequest();
            SetupValidationSuccess(request);

            var existingCart = new API.Entities.Cart
            {
                Id = request.CartId,
                Items = new List<CartItem> { new CartItem { ProductId = Guid.NewGuid() } },
            };

            _repositoryMock
                .Setup(r => r.GetCartAsync(request.CartId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingCart);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Error.Should().BeOfType<NotFound>();
            result.Error.Message.Should().Be("Cart item not found");
        }

        [Fact]
        public async Task Handle_Should_Update_Quantity_And_Publish_Event_When_Success()
        {
            // Arrange
            var request = CreateValidRequest();
            SetupValidationSuccess(request);

            var existingItem = new CartItem
            {
                ProductId = request.Dto.ProductId,
                ProductName = "Test Product",
                Quantity = 1,
                UnitPrice = 50,
            };

            var existingCart = new API.Entities.Cart
            {
                Id = request.CartId,
                UserId = request.UserId,
                Items = new List<CartItem> { existingItem },
            };

            _repositoryMock
                .Setup(r => r.GetCartAsync(request.CartId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingCart);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Value.Items.First().Quantity.Should().Be(request.Dto.Quantity);

            _repositoryMock.Verify(
                r =>
                    r.StoreCartAsync(
                        It.Is<API.Entities.Cart>(c =>
                            c.Items.First().Quantity == request.Dto.Quantity
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );

            _eventPublisherMock.Verify(
                e =>
                    e.PublishAsync(
                        It.Is<CartItemUpdatedEvent>(evt =>
                            evt.CartId == request.CartId
                            && evt.ProductId == request.Dto.ProductId
                            && evt.Quantity == request.Dto.Quantity
                        ),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        private void SetupValidationSuccess(UpdateCartItem.Request request)
        {
            _validatorMock
                .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
        }

        private static UpdateCartItem.Request CreateValidRequest()
        {
            return new UpdateCartItem.Request(
                CartId: Guid.NewGuid(),
                UserId: Guid.NewGuid(),
                Dto: new UpdateCartItem.RequestDto(ProductId: Guid.NewGuid(), Quantity: 10)
            );
        }
    }
}
