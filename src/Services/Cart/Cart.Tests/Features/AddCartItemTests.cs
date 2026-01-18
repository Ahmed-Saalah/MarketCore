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

namespace Cart.API.Tests.Features;

public class AddItemTests
{
    public class ValidatorTests
    {
        private readonly AddItem.Validator _validator = new();

        [Fact]
        public void Should_Have_Error_When_ProductId_Is_Empty()
        {
            var model = CreateRequest(productId: Guid.Empty);
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Item.ProductId);
        }

        [Fact]
        public void Should_Have_Error_When_Quantity_Is_Zero_Or_Less()
        {
            var model = CreateRequest(quantity: 0);
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Item.Quantity);
        }

        [Fact]
        public void Should_Have_Error_When_UnitPrice_Is_Negative()
        {
            var model = CreateRequest(unitPrice: -10m);
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Item.UnitPrice);
        }

        [Fact]
        public void Should_Not_Have_Error_When_Request_Is_Valid()
        {
            var model = CreateRequest();
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveAnyValidationErrors();
        }

        private static AddItem.Request CreateRequest(
            Guid? productId = null,
            int quantity = 1,
            decimal unitPrice = 100
        )
        {
            return new AddItem.Request(
                CartId: Guid.NewGuid(),
                UserId: Guid.NewGuid(),
                StoreId: Guid.NewGuid(),
                Item: new AddItem.AddItemDto(
                    ProductId: productId ?? Guid.NewGuid(),
                    ProductName: "Test Product",
                    UnitPrice: unitPrice,
                    Quantity: quantity,
                    PictureUrl: "http://image.com"
                )
            );
        }
    }

    public class HandlerTests
    {
        private readonly Mock<ICartRepository> _repositoryMock;
        private readonly Mock<IValidator<AddItem.Request>> _validatorMock;
        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly AddItem.Handler _handler;

        public HandlerTests()
        {
            _repositoryMock = new Mock<ICartRepository>();
            _validatorMock = new Mock<IValidator<AddItem.Request>>();
            _eventPublisherMock = new Mock<IEventPublisher>();
            _handler = new AddItem.Handler(
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
            var validationFailure = new ValidationFailure("Prop", "Error");
            var failedResult = new ValidationResult(new[] { validationFailure });

            _validatorMock
                .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(failedResult);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Error.Should().NotBeNull();
            result.Error.Should().BeOfType<ValidationError>();

            _repositoryMock.Verify(
                x =>
                    x.StoreCartAsync(
                        It.IsAny<Cart.API.Entities.Cart>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_Create_New_Cart_When_Cart_Does_Not_Exist()
        {
            // Arrange
            var request = CreateValidRequest();
            SetupValidationSuccess(request);

            _repositoryMock
                .Setup(x =>
                    x.GetCartByUserIdAsync(request.UserId!.Value, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync((Cart.API.Entities.Cart?)null);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Value.Should().NotBeNull();

            _repositoryMock.Verify(
                x =>
                    x.StoreCartAsync(
                        It.Is<Cart.API.Entities.Cart>(c =>
                            c.UserId == request.UserId
                            && c.Items.Count == 1
                            && c.Items.First().ProductId == request.Item.ProductId
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Add_New_Item_To_Existing_Cart()
        {
            // Arrange
            var request = CreateValidRequest();
            SetupValidationSuccess(request);

            var existingCart = new Cart.API.Entities.Cart
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Items = new List<CartItem>(),
            };

            _repositoryMock
                .Setup(x =>
                    x.GetCartByUserIdAsync(request.UserId!.Value, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(existingCart);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Value.Items.Should().HaveCount(1);

            _repositoryMock.Verify(
                x =>
                    x.StoreCartAsync(
                        It.Is<Cart.API.Entities.Cart>(c => c.Items.Count == 1),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Update_Quantity_When_Item_Already_Exists()
        {
            // Arrange
            var request = CreateValidRequest();
            SetupValidationSuccess(request);

            var existingProductId = request.Item.ProductId;

            var existingCart = new Entities.Cart
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Items = new List<CartItem>
                {
                    new CartItem
                    {
                        ProductId = existingProductId,
                        Quantity = 5,
                        UnitPrice = 50,
                    },
                },
            };

            _repositoryMock
                .Setup(x =>
                    x.GetCartByUserIdAsync(request.UserId!.Value, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(existingCart);

            // Act
            await _handler.Handle(request, CancellationToken.None);

            // Assert
            var expectedQty = 5 + request.Item.Quantity;

            _repositoryMock.Verify(
                x =>
                    x.StoreCartAsync(
                        It.Is<Cart.API.Entities.Cart>(c =>
                            c.Items.First().Quantity == expectedQty
                            && c.Items.First().UnitPrice == request.Item.UnitPrice
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Publish_CartItemAddedEvent()
        {
            // Arrange
            var request = CreateValidRequest();
            SetupValidationSuccess(request);

            _repositoryMock
                .Setup(x => x.GetCartByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Cart.API.Entities.Cart?)null);

            // Act
            await _handler.Handle(request, CancellationToken.None);

            // Assert
            _eventPublisherMock.Verify(
                x =>
                    x.PublishAsync(
                        It.Is<CartItemAddedEvent>(e =>
                            e.ProductId == request.Item.ProductId
                            && e.Quantity == request.Item.Quantity
                        ),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_LookUp_By_CartId_When_UserId_Is_Null()
        {
            // Arrange
            var cartId = Guid.NewGuid();
            var request = new AddItem.Request(
                cartId,
                null,
                Guid.NewGuid(),
                new AddItem.AddItemDto(Guid.NewGuid(), "P", 10, 1, null)
            );

            SetupValidationSuccess(request);

            // Act
            await _handler.Handle(request, CancellationToken.None);

            // Assert
            _repositoryMock.Verify(
                x => x.GetCartAsync(cartId, It.IsAny<CancellationToken>()),
                Times.Once
            );

            _repositoryMock.Verify(
                x => x.GetCartByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        private void SetupValidationSuccess(AddItem.Request request)
        {
            _validatorMock
                .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
        }

        private static AddItem.Request CreateValidRequest()
        {
            return new AddItem.Request(
                CartId: Guid.NewGuid(),
                UserId: Guid.NewGuid(),
                StoreId: Guid.NewGuid(),
                Item: new AddItem.AddItemDto(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Unit Test Product",
                    UnitPrice: 100m,
                    Quantity: 2,
                    PictureUrl: "test.jpg"
                )
            );
        }
    }
}
