using System.Security.Claims;
using Core.Domain.Errors;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Store.API.Data;
using Store.API.Features;

namespace Store.Tests.Features;

public class GetMyStoreTests
{
    private readonly StoreDbContext _dbContext;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly GetMyStore.Handler _handler;

    public GetMyStoreTests()
    {
        var options = new DbContextOptionsBuilder<StoreDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new StoreDbContext(options);
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _handler = new GetMyStore.Handler(_dbContext, _httpContextAccessorMock.Object);
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
    public async Task Handle_ShouldReturnStore_WhenStoreExists()
    {
        // Arrange
        int userId = 101;
        var existingStore = new API.Entities.Store
        {
            Id = Guid.NewGuid(),
            OwnerIdentityId = userId,
            Name = "My Tech Shop",
            Description = "Best gadgets",
            IsActive = true,
        };
        _dbContext.Stores.Add(existingStore);
        await _dbContext.SaveChangesAsync();

        SetupUser(userId);

        // Act
        var result = await _handler.Handle(new GetMyStore.Request(), CancellationToken.None);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be("My Tech Shop");
        result.Value.Id.Should().Be(existingStore.Id);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenUserHasNoStore()
    {
        // Arrange
        SetupUser(999);

        // Act
        var result = await _handler.Handle(new GetMyStore.Request(), CancellationToken.None);

        // Assert
        result.Error.Should().BeOfType<NotFound>();
    }
}
