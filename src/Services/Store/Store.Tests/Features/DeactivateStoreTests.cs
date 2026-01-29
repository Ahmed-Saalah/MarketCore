using System.Security.Claims;
using Core.Domain.Errors;
using Core.Messaging;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Store.API.Data;
using Store.API.Features;
using Store.API.Messages;

namespace Store.API.Tests.Features;

public class DeactivateStoreTests
{
    private readonly StoreDbContext _dbContext;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly DeactivateStore.Handler _handler;

    public DeactivateStoreTests()
    {
        var options = new DbContextOptionsBuilder<StoreDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new StoreDbContext(options);

        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _eventPublisherMock = new Mock<IEventPublisher>();

        _handler = new DeactivateStore.Handler(
            _dbContext,
            _eventPublisherMock.Object,
            _httpContextAccessorMock.Object
        );
    }

    private void SetupUser(int identityId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, identityId.ToString()) };
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims)),
        };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
    }

    [Fact]
    public async Task Handle_ShouldDeactivateAndPublishEvent_WhenStoreExists()
    {
        // Arrange
        int userId = 303;
        var storeId = Guid.NewGuid();
        var activeStore = new Entities.Store
        {
            Id = storeId,
            OwnerIdentityId = userId,
            Name = "Closing Shop",
            IsActive = true,
            OwnerEmail = "test@shop.com",
        };
        _dbContext.Stores.Add(activeStore);
        await _dbContext.SaveChangesAsync();

        SetupUser(userId);

        // Act
        var result = await _handler.Handle(new DeactivateStore.Request(), CancellationToken.None);

        // Assert
        var dbStore = await _dbContext.Stores.FindAsync(storeId);
        dbStore.IsActive.Should().BeFalse();

        _eventPublisherMock.Verify(
            x =>
                x.PublishAsync(
                    It.Is<StoreDeactivatedEvent>(e =>
                        e.StoreId == storeId
                        && e.OwnerIdentityId == userId
                        && e.StoreName == "Closing Shop"
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenTokenIsMissing()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());

        // Act
        var result = await _handler.Handle(new DeactivateStore.Request(), CancellationToken.None);

        // Assert
        result.Error.Should().NotBeNull();
        result.Error.Should().BeOfType<UnauthorizedError>();

        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenStoreDoesNotExist()
    {
        // Arrange
        SetupUser(555);

        // Act
        var result = await _handler.Handle(new DeactivateStore.Request(), CancellationToken.None);

        // Assert
        result.Error.Should().NotBeNull();
        result.Error.Should().BeOfType<NotFound>();

        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
}
