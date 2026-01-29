using Catalog.API.Data;
using Catalog.API.Entities.Products;
using Catalog.API.Features.Products;
using Catalog.API.Messages.Products;
using Core.Domain.Errors;
using Core.Messaging;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Catalog.Tests.Features.Products;

public class DeactivateProductTests
{
    public class HandlerTests
    {
        private readonly CatalogDbContext _dbContext;
        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly DeactivateProduct.Handler _handler;

        public HandlerTests()
        {
            var options = new DbContextOptionsBuilder<CatalogDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new CatalogDbContext(options);
            _eventPublisherMock = new Mock<IEventPublisher>();

            _handler = new DeactivateProduct.Handler(_dbContext, _eventPublisherMock.Object);
        }

        [Fact]
        public async Task Handle_Should_Deactivate_Product_And_Publish_Event()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var storeId = Guid.NewGuid();

            var product = new Product
            {
                Id = productId,
                StoreId = storeId,
                Name = "Active Product",
                IsActive = true,
                Sku = "ACTIVE-SKU",
                Description = "An active product for testing",
            };

            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();

            var request = new DeactivateProduct.Request(productId, storeId);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            var dbProduct = await _dbContext.Products.FindAsync(productId);
            dbProduct!.IsActive.Should().BeFalse();
            _eventPublisherMock.Verify(
                x =>
                    x.PublishAsync(
                        It.Is<ProductDeactivatedEvent>(e => e.ProductId == productId),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_Product_Does_Not_Exist()
        {
            // Arrange
            var request = new DeactivateProduct.Request(Guid.NewGuid(), Guid.NewGuid());

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Error.Should().BeOfType<NotFound>();
            result.Error.Message.Should().Be("Product not found");
        }

        [Fact]
        public async Task Handle_Should_Return_Forbidden_When_StoreId_Does_Not_Match()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var ownerStoreId = Guid.NewGuid();
            var attackerStoreId = Guid.NewGuid();

            var product = new Product
            {
                Id = productId,
                StoreId = ownerStoreId,
                IsActive = true,
                Sku = "ACTIVE-SKU",
                Name = "Active Product",
                Description = "An active product for testing",
            };

            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();

            var request = new DeactivateProduct.Request(productId, attackerStoreId);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Error.Should().BeOfType<ForbiddenError>();
            var dbProduct = await _dbContext.Products.FindAsync(productId);
            dbProduct!.IsActive.Should().BeTrue();
            _eventPublisherMock.Verify(
                x =>
                    x.PublishAsync(
                        It.IsAny<ProductDeactivatedEvent>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
        }
    }
}
