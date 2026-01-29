using Cart.API.Data;
using Cart.API.Entities;
using Cart.API.Features;
using Cart.API.Messages;
using Core.Domain.Errors;
using Core.Messaging;
using FluentAssertions;
using Moq;

namespace Cart.Tests.Features;

public class RemoveItemTests
{
    public class HandlerTests
    {
        private readonly Mock<ICartRepository> _repositoryMock;
        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly RemoveItem.Handler _handler;

        public HandlerTests()
        {
            _repositoryMock = new Mock<ICartRepository>();
            _eventPublisherMock = new Mock<IEventPublisher>();

            _handler = new RemoveItem.Handler(_repositoryMock.Object, _eventPublisherMock.Object);
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_Cart_Does_Not_Exist()
        {
            // Arrange
            var request = new RemoveItem.Request(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

            _repositoryMock
                .Setup(x => x.GetCartAsync(request.CartId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((API.Entities.Cart?)null);

            _repositoryMock
                .Setup(x =>
                    x.GetCartByUserIdAsync(request.UserId!.Value, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync((API.Entities.Cart?)null);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Error.Should().NotBeNull();
            result.Error.Should().BeOfType<NotFound>();
            result.Error.Message.Should().Be("Cart not found");

            _repositoryMock.Verify(
                x => x.StoreCartAsync(It.IsAny<API.Entities.Cart>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_Item_Does_Not_Exist_In_Cart()
        {
            // Arrange
            var cartId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var request = new RemoveItem.Request(cartId, null, productId);

            var existingCart = new API.Entities.Cart
            {
                Id = cartId,
                Items = new List<CartItem> { new CartItem { ProductId = Guid.NewGuid() } },
            };

            _repositoryMock
                .Setup(x => x.GetCartAsync(request.CartId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingCart);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Error.Should().NotBeNull();
            result.Error.Should().BeOfType<NotFound>();
            result.Error.Message.Should().Be("Cart item not found");
        }

        [Fact]
        public async Task Handle_Should_Remove_Item_And_Publish_Event_When_Found_By_CartId()
        {
            // Arrange
            var cartId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var request = new RemoveItem.Request(cartId, null, productId);

            var itemToRemove = new CartItem
            {
                ProductId = productId,
                ProductName = "Test Item",
                Quantity = 5,
                UnitPrice = 100,
            };

            var existingCart = new API.Entities.Cart
            {
                Id = cartId,
                Items = new List<CartItem> { itemToRemove },
            };

            _repositoryMock
                .Setup(x => x.GetCartAsync(request.CartId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingCart);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Value.Should().BeTrue();

            _repositoryMock.Verify(
                x =>
                    x.StoreCartAsync(
                        It.Is<API.Entities.Cart>(c => c.Items.Count == 0),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );

            _eventPublisherMock.Verify(
                x =>
                    x.PublishAsync(
                        It.Is<CartItemRemovedEvent>(e =>
                            e.CartId == cartId && e.ProductId == productId && e.Quantity == 5
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_LookUp_By_UserId_If_CartId_Not_Found()
        {
            // Arrange
            var cartId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var request = new RemoveItem.Request(cartId, userId, productId);

            var itemToRemove = new CartItem { ProductId = productId };
            var existingCart = new API.Entities.Cart
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Items = new List<CartItem> { itemToRemove },
            };

            _repositoryMock
                .Setup(x => x.GetCartAsync(cartId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((API.Entities.Cart?)null);

            _repositoryMock
                .Setup(x => x.GetCartByUserIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingCart);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            _repositoryMock.Verify(
                x => x.GetCartByUserIdAsync(userId, It.IsAny<CancellationToken>()),
                Times.Once
            );

            _repositoryMock.Verify(
                x =>
                    x.StoreCartAsync(
                        It.Is<API.Entities.Cart>(c => c.Items.Count == 0),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }
    }
}
