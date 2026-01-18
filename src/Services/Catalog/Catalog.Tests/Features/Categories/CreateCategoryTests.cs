using Catalog.API.Data;
using Catalog.API.Entities.Categories;
using Catalog.API.Features.Categories;
using Catalog.API.Messages.Categories;
using Core.Domain.Errors;
using Core.Messaging;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Catalog.Tests.Features.Categories;

public class CreateCategoryTests
{
    public class ValidatorTests
    {
        private readonly CreateCategory.Validator _validator = new();

        [Fact]
        public void Should_Have_Error_When_Name_Is_Empty()
        {
            var request = new CreateCategory.Request("", "Description", null);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Have_Error_When_Name_Exceeds_Length()
        {
            var request = new CreateCategory.Request(new string('a', 101), "Description", null);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Not_Have_Error_When_Request_Is_Valid()
        {
            var request = new CreateCategory.Request("Valid Name", "Valid Description", null);
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }

    public class HandlerTests
    {
        private readonly CatalogDbContext _dbContext;
        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly CreateCategory.Handler _handler;

        public HandlerTests()
        {
            // 1. Setup In-Memory Database
            //
            var options = new DbContextOptionsBuilder<CatalogDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new CatalogDbContext(options);
            _eventPublisherMock = new Mock<IEventPublisher>();
            _handler = new CreateCategory.Handler(_dbContext, _eventPublisherMock.Object);
        }

        [Fact]
        public async Task Handle_Should_Create_Category_When_ParentId_Is_Null()
        {
            // Arrange
            var request = new CreateCategory.Request("Root Cat", "Desc", null);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Value.Should().NotBeEmpty();
            var savedCategory = await _dbContext.Categories.FirstAsync();
            savedCategory.Name.Should().Be("Root Cat");
            savedCategory.ParentId.Should().BeNull();
            _eventPublisherMock.Verify(
                x =>
                    x.PublishAsync(
                        It.Is<CategoryCreatedEvent>(e => e.Name == request.Name),
                        "catalog.category.created",
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_Parent_Does_Not_Exist()
        {
            var parentId = Guid.NewGuid();
            var request = new CreateCategory.Request("Child Cat", "Desc", parentId);
            var result = await _handler.Handle(request, CancellationToken.None);
            result.Error.Should().BeOfType<NotFound>();
            var count = await _dbContext.Categories.CountAsync();
            count.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Should_Create_Category_When_Parent_Exists()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            _dbContext.Categories.Add(new Category { Id = parentId, Name = "Parent" });
            await _dbContext.SaveChangesAsync();
            var request = new CreateCategory.Request("Child Cat", "Desc", parentId);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            var savedChild = await _dbContext.Categories.FirstOrDefaultAsync(c =>
                c.Name == "Child Cat"
            );
            savedChild.Should().NotBeNull();
            savedChild!.ParentId.Should().Be(parentId);

            _eventPublisherMock.Verify(
                x =>
                    x.PublishAsync(
                        It.IsAny<CategoryCreatedEvent>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }
    }
}
