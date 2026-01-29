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

public class UpdateCategoryTests
{
    public class ValidatorTests
    {
        private readonly UpdateCategory.Validator _validator = new();

        [Fact]
        public void Should_Have_Error_When_Name_Is_Empty()
        {
            var request = new UpdateCategory.Request(Guid.NewGuid(), "", "Desc", null);
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Have_Error_When_ParentId_Equals_Id()
        {
            var id = Guid.NewGuid();
            var request = new UpdateCategory.Request(id, "Name", "Desc", id);

            var result = _validator.TestValidate(request);

            result.ShouldHaveValidationErrorFor(x => x);
            result
                .Errors.Should()
                .Contain(e => e.ErrorMessage == "A category cannot be its own parent.");
        }

        [Fact]
        public void Should_Not_Have_Error_When_Request_Is_Valid()
        {
            var request = new UpdateCategory.Request(
                Guid.NewGuid(),
                "Valid Name",
                "Valid Desc",
                Guid.NewGuid()
            );
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }

    public class HandlerTests
    {
        private readonly CatalogDbContext _dbContext;
        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly UpdateCategory.Handler _handler;

        public HandlerTests()
        {
            var options = new DbContextOptionsBuilder<CatalogDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new CatalogDbContext(options);
            _eventPublisherMock = new Mock<IEventPublisher>();

            _handler = new UpdateCategory.Handler(_dbContext, _eventPublisherMock.Object);
        }

        [Fact]
        public async Task Handle_Should_Update_Category_Details()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var initialCategory = new Category
            {
                Id = categoryId,
                Name = "Old Name",
                Description = "Old Desc",
                ParentId = null,
            };

            _dbContext.Categories.Add(initialCategory);
            await _dbContext.SaveChangesAsync();

            var request = new UpdateCategory.Request(categoryId, "New Name", "New Desc", null);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            var updatedCategory = await _dbContext.Categories.FindAsync(categoryId);
            updatedCategory!.Name.Should().Be("New Name");
            updatedCategory.Description.Should().Be("New Desc");
            _eventPublisherMock.Verify(
                x =>
                    x.PublishAsync(
                        It.Is<CategoryUpdatedEvent>(e =>
                            e.Name == "New Name" && e.Description == "New Desc"
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_Category_Does_Not_Exist()
        {
            // Arrange
            var request = new UpdateCategory.Request(Guid.NewGuid(), "Name", "Desc", null);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Error.Should().BeOfType<NotFound>();
            result.Error.Message.Should().Be("Category not found.");
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_When_New_Parent_Does_Not_Exist()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var nonExistentParentId = Guid.NewGuid();

            _dbContext.Categories.Add(new Category { Id = categoryId, Name = "Cat" });
            await _dbContext.SaveChangesAsync();
            var request = new UpdateCategory.Request(
                categoryId,
                "Name",
                "Desc",
                nonExistentParentId
            );

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Error.Should().BeOfType<NotFound>();
            result.Error.Message.Should().Be("Parent category does not exist.");

            var dbCategory = await _dbContext.Categories.FindAsync(categoryId);
            dbCategory!.ParentId.Should().BeNull();
        }

        [Fact]
        public async Task Handle_Should_Update_Parent_When_Parent_Exists()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var parentId = Guid.NewGuid();

            _dbContext.Categories.AddRange(
                new Category { Id = categoryId, Name = "Child" },
                new Category { Id = parentId, Name = "Parent" }
            );
            await _dbContext.SaveChangesAsync();

            var request = new UpdateCategory.Request(categoryId, "Child Updated", "Desc", parentId);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            var dbCategory = await _dbContext.Categories.FindAsync(categoryId);
            dbCategory!.ParentId.Should().Be(parentId);
        }
    }
}
