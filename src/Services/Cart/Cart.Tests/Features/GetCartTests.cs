using Cart.API.Data;
using Cart.API.Entities;
using Cart.API.Features;
using FluentAssertions;
using Moq;

namespace Cart.Tests.Features;

public sealed class GetCartTests
{
    public class HandlerTests
    {
        private readonly Mock<ICartRepository> _repositoryMock;
        private readonly GetCart.Handler _handler;

        public HandlerTests()
        {
            _repositoryMock = new Mock<ICartRepository>();
            _handler = new GetCart.Handler(_repositoryMock.Object);
        }

        [Fact]
        public async Task Handle_Should_Return_Cart_When_Found_By_UserId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new GetCart.Request(CartId: null, UserId: userId);

            var existingCart = new API.Entities.Cart
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Items = new List<CartItem>
                {
                    new CartItem
                    {
                        ProductName = "Item 1",
                        Quantity = 1,
                        UnitPrice = 10,
                    },
                    new CartItem
                    {
                        ProductName = "Item 2",
                        Quantity = 2,
                        UnitPrice = 20,
                    },
                },
            };

            _repositoryMock
                .Setup(x => x.GetCartByUserIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingCart);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Value.Should().NotBeNull();
            result.Value.UserId.Should().Be(userId);
            result.Value.Items.Should().HaveCount(2);
            result.Value.TotalPrice.Should().Be(existingCart.TotalPrice);

            _repositoryMock.Verify(
                x => x.GetCartByUserIdAsync(userId, It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Return_Cart_When_Found_By_CartId_And_UserId_Is_Null()
        {
            // Arrange
            var cartId = Guid.NewGuid();
            var request = new GetCart.Request(CartId: cartId, UserId: null);

            var existingCart = new API.Entities.Cart
            {
                Id = cartId,
                Items = new List<CartItem>
                {
                    new CartItem
                    {
                        ProductName = "Guest Item",
                        Quantity = 1,
                        UnitPrice = 50,
                    },
                },
            };

            _repositoryMock
                .Setup(x => x.GetCartAsync(cartId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingCart);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Value.Id.Should().Be(cartId);
            result.Value.Items.Should().HaveCount(1);

            _repositoryMock.Verify(
                x => x.GetCartAsync(cartId, It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Prioritize_UserId_Over_CartId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var cartId = Guid.NewGuid();
            var request = new GetCart.Request(CartId: cartId, UserId: userId);
            var userCart = new API.Entities.Cart { Id = Guid.NewGuid(), UserId = userId };
            _repositoryMock
                .Setup(x => x.GetCartByUserIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userCart);

            // Act
            await _handler.Handle(request, CancellationToken.None);

            // Assert
            _repositoryMock.Verify(
                x => x.GetCartByUserIdAsync(userId, It.IsAny<CancellationToken>()),
                Times.Once
            );

            _repositoryMock.Verify(
                x => x.GetCartAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_Return_Empty_Dto_When_Cart_Not_Found()
        {
            // Arrange
            var cartId = Guid.NewGuid();
            var request = new GetCart.Request(CartId: cartId, UserId: null);

            _repositoryMock
                .Setup(x => x.GetCartAsync(cartId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((API.Entities.Cart?)null);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Value.Should().NotBeNull();

            // Verify empty state
            result.Value.Items.Should().BeEmpty();
            result.Value.TotalPrice.Should().Be(0);
            result.Value.Id.Should().Be(cartId);
        }

        [Fact]
        public async Task Handle_Should_Generate_New_Id_When_Not_Found_And_Request_Id_Is_Null()
        {
            // Arrange
            // No CartId, No UserId
            var request = new GetCart.Request(CartId: null, UserId: null);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Value.Items.Should().BeEmpty();
            result.Value.Id.Should().NotBeEmpty(); // A new Guid should be generated

            // Verify repo was not queried (since IDs were null)
            _repositoryMock.Verify(
                x => x.GetCartAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
            _repositoryMock.Verify(
                x => x.GetCartByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }
    }
}
