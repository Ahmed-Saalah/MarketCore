using Catalog.API.Data;
using Catalog.API.Entities.Products;
using Catalog.API.Features.Products;
using Catalog.API.Messages.Products;
using Core.Domain.Errors;
using Core.Messaging;
using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Catalog.Tests.Features.Products;

public class UpdateProductTests
{
    public class ValidatorTests
    {
        private readonly UpdateProduct.Validator _validator = new();

        [Fact]
        public void Should_Have_Error_When_Name_Is_Empty()
        {
            var dto = CreateValidDto() with { Name = "" };
            var request = new UpdateProduct.Request(Guid.NewGuid(), Guid.NewGuid(), dto);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Data.Name);
        }

        [Fact]
        public void Should_Have_Error_When_Price_Is_Zero()
        {
            var dto = CreateValidDto() with { Price = 0 };
            var request = new UpdateProduct.Request(Guid.NewGuid(), Guid.NewGuid(), dto);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Data.Price);
        }

        [Fact]
        public void Should_Not_Have_Error_When_Request_Is_Valid()
        {
            var dto = CreateValidDto();
            var request = new UpdateProduct.Request(Guid.NewGuid(), Guid.NewGuid(), dto);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveAnyValidationErrors();
        }

        private static UpdateProduct.RequestDto CreateValidDto()
        {
            return new UpdateProduct.RequestDto(
                Name: "Valid Name",
                Description: "Desc",
                Price: 100m,
                Sku: "SKU-123",
                Images: [],
                Attributes: []
            );
        }
    }

    public class HandlerTests
    {
        private readonly CatalogDbContext _dbContext;
        private readonly Mock<IValidator<UpdateProduct.Request>> _validatorMock;
        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly UpdateProduct.Handler _handler;

        public HandlerTests()
        {
            var options = new DbContextOptionsBuilder<CatalogDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new CatalogDbContext(options);
            _validatorMock = new Mock<IValidator<UpdateProduct.Request>>();

            _validatorMock
                .Setup(v =>
                    v.ValidateAsync(
                        It.IsAny<UpdateProduct.Request>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());
            _eventPublisherMock = new Mock<IEventPublisher>();

            _handler = new UpdateProduct.Handler(
                _dbContext,
                _validatorMock.Object,
                _eventPublisherMock.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Update_Product_Properties_And_Replace_Collections()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var storeId = Guid.NewGuid();
            var existingProduct = new Product
            {
                Id = productId,
                StoreId = storeId,
                Name = "Old Name",
                Price = 50m,
                Images = [new ProductImage { ImageUrl = "old.jpg", IsPrimary = true }],
                Attributes = [new ProductAttribute { Key = "Color", Value = "Red" }],
                Sku = "OLD-SKU",
                Description = "Old Desc",
            };

            _dbContext.Products.Add(existingProduct);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();
            var dto = new UpdateProduct.RequestDto(
                Name: "New Name",
                Description: "New Desc",
                Price: 99.99m,
                Sku: "NEW-SKU",
                Images: [new("new.jpg", true)],
                Attributes: [new("Size", "L")]
            );

            var request = new UpdateProduct.Request(productId, storeId, dto);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            var updatedProduct = await _dbContext
                .Products.Include(p => p.Images)
                .Include(p => p.Attributes)
                .FirstOrDefaultAsync(p => p.Id == productId);

            updatedProduct.Should().NotBeNull();
            updatedProduct!.Name.Should().Be("New Name");
            updatedProduct.Price.Should().Be(99.99m);

            updatedProduct.Images.Should().HaveCount(1);
            updatedProduct.Images.First().ImageUrl.Should().Be("new.jpg");

            updatedProduct.Attributes.Should().HaveCount(1);
            updatedProduct.Attributes.First().Key.Should().Be("Size");

            _eventPublisherMock.Verify(
                x =>
                    x.PublishAsync(
                        It.Is<ProductUpdatedEvent>(e =>
                            e.Id == productId && e.Name == "New Name" && e.Price == 99.99m
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_Product_Does_Not_Exist()
        {
            // Arrange
            var dto = new UpdateProduct.RequestDto("Name", "Desc", 10, "SKU", [], []);
            var request = new UpdateProduct.Request(Guid.NewGuid(), Guid.NewGuid(), dto);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Error.Should().BeOfType<NotFound>();
            result.Error.Message.Should().Be("Product not found");
        }

        [Fact]
        public async Task Handle_Should_Return_Forbidden_When_StoreId_Mismatch()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var ownerStoreId = Guid.NewGuid();
            var attackerStoreId = Guid.NewGuid();

            var product = new Product
            {
                Id = productId,
                StoreId = ownerStoreId,
                Name = "Original Name",
                Price = 100,
                Sku = "SKU",
                Description = "Original Desc",
            };

            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();

            var dto = new UpdateProduct.RequestDto("Hacked Name", "Desc", 1, "SKU", [], []);
            var request = new UpdateProduct.Request(productId, attackerStoreId, dto);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Error.Should().BeOfType<ForbiddenError>();

            // Verify DB was NOT updated
            var dbProduct = await _dbContext.Products.FindAsync(productId);
            dbProduct!.Name.Should().Be("Original Name");
        }
    }
}
