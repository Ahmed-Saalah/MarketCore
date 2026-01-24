using Catalog.API.Data;
using Catalog.API.Entities.Categories;
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

namespace Catalog.Tests.Features.Products;

public class CreateProductTests
{
    public class ValidatorTests
    {
        private readonly CreateProduct.Validator _validator = new();

        [Fact]
        public void Should_Have_Error_When_Name_Is_Empty()
        {
            var dto = CreateValidDto() with { Name = "" };
            var request = new CreateProduct.Request(Guid.NewGuid(), dto);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Data.Name);
        }

        [Fact]
        public void Should_Have_Error_When_Sku_Is_Short()
        {
            var dto = CreateValidDto() with { Sku = "AB" };
            var request = new CreateProduct.Request(Guid.NewGuid(), dto);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Data.Sku);
        }

        [Fact]
        public void Should_Have_Error_When_Price_Is_Zero()
        {
            var dto = CreateValidDto() with { Price = 0 };
            var request = new CreateProduct.Request(Guid.NewGuid(), dto);

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x.Data.Price);
        }

        [Fact]
        public void Should_Not_Have_Error_When_Request_Is_Valid()
        {
            var dto = CreateValidDto();
            var request = new CreateProduct.Request(Guid.NewGuid(), dto);

            var result = _validator.TestValidate(request);

            result.ShouldNotHaveAnyValidationErrors();
        }

        private static CreateProduct.RequestDto CreateValidDto()
        {
            return new CreateProduct.RequestDto(
                CategoryId: Guid.NewGuid(),
                Name: "Test Product",
                Description: "Desc",
                Sku: "SKU-123",
                Price: 100,
                Currency: "USD",
                Images: [],
                Attributes: []
            );
        }
    }

    public class HandlerTests
    {
        private readonly CatalogDbContext _dbContext;
        private readonly Mock<IValidator<CreateProduct.Request>> _validatorMock;
        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly CreateProduct.Handler _handler;

        public HandlerTests()
        {
            var options = new DbContextOptionsBuilder<CatalogDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new CatalogDbContext(options);
            _validatorMock = new Mock<IValidator<CreateProduct.Request>>();

            _validatorMock
                .Setup(v =>
                    v.ValidateAsync(
                        It.IsAny<CreateProduct.Request>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());
            _eventPublisherMock = new Mock<IEventPublisher>();
            _handler = new CreateProduct.Handler(
                _dbContext,
                _validatorMock.Object,
                _eventPublisherMock.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Create_Product_With_Images_And_Attributes()
        {
            // Arrange
            var storeId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();

            _dbContext.Categories.Add(new Category { Id = categoryId, Name = "Electronics" });
            await _dbContext.SaveChangesAsync();

            var dto = new CreateProduct.RequestDto(
                CategoryId: categoryId,
                Name: "Gaming Laptop",
                Description: "Fast laptop",
                Sku: "LAP-001",
                Price: 1500,
                Currency: "USD",
                Images: [new("img1.jpg", true)],
                Attributes: [new("RAM", "16GB")]
            );

            var request = new CreateProduct.Request(storeId, dto);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Value.Should().NotBeEmpty();

            var savedProduct = await _dbContext
                .Products.Include(p => p.Images)
                .Include(p => p.Attributes)
                .FirstOrDefaultAsync(p => p.Id == result.Value);

            savedProduct.Should().NotBeNull();
            savedProduct!.StoreId.Should().Be(storeId);
            savedProduct.Name.Should().Be("Gaming Laptop");
            savedProduct.IsActive.Should().BeTrue();
            savedProduct.Images.Should().HaveCount(1);
            savedProduct.Images.First().ImageUrl.Should().Be("img1.jpg");
            savedProduct.Images.First().IsPrimary.Should().BeTrue();

            savedProduct.Attributes.Should().HaveCount(1);
            savedProduct.Attributes.First().Key.Should().Be("RAM");
            savedProduct.Attributes.First().Value.Should().Be("16GB");

            _eventPublisherMock.Verify(
                x =>
                    x.PublishAsync(
                        It.Is<ProductCreatedEvent>(e =>
                            e.Name == "Gaming Laptop"
                            && e.CategoryName == "Electronics"
                            && e.Sku == "LAP-001"
                        ),
                        "Catalog.ProductCreatedEvent",
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Return_Conflict_When_Sku_Already_Exists()
        {
            // Arrange
            var existingSku = "DUPLICATE-SKU";
            _dbContext.Products.Add(
                new Product
                {
                    Id = Guid.NewGuid(),
                    Sku = existingSku,
                    Name = "prd1",
                    Description = "ped-desc",
                }
            );
            await _dbContext.SaveChangesAsync();

            var dto = new CreateProduct.RequestDto(
                CategoryId: Guid.NewGuid(),
                Name: "New Product",
                Description: "Desc",
                Sku: existingSku, // Same SKU
                Price: 100,
                Currency: "USD",
                Images: [],
                Attributes: []
            );

            var request = new CreateProduct.Request(Guid.NewGuid(), dto);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Error.Should().BeOfType<ConflictError>();
            result.Error.Message.Should().Be("This SKU is already in use.");

            // Verify no new product added (Count should remain 1)
            (await _dbContext.Products.CountAsync())
                .Should()
                .Be(1);
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_Category_Does_Not_Exist()
        {
            // Arrange
            var nonExistentCategoryId = Guid.NewGuid();

            var dto = new CreateProduct.RequestDto(
                CategoryId: nonExistentCategoryId,
                Name: "Product",
                Description: "Desc",
                Sku: "NEW-SKU",
                Price: 100,
                Currency: "USD",
                Images: [],
                Attributes: []
            );

            var request = new CreateProduct.Request(Guid.NewGuid(), dto);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Error.Should().BeOfType<NotFound>();
            result.Error.Message.Should().Be("Category not found.");
        }
    }
}
