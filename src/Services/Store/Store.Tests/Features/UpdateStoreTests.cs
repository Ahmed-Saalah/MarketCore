using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Store.API.Data;
using Store.API.Features;

namespace Store.Tests.Features;

public class UpdateStoreTests
{
    private readonly StoreDbContext _dbContext;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly UpdateStore.Handler _handler;

    public UpdateStoreTests()
    {
        var options = new DbContextOptionsBuilder<StoreDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new StoreDbContext(options);
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _handler = new UpdateStore.Handler(_dbContext, _httpContextAccessorMock.Object);
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
    public async Task Handle_ShouldUpdateFields_WhenStoreExists()
    {
        // Arrange
        int userId = 202;
        var storeId = Guid.NewGuid();
        var originalStore = new API.Entities.Store
        {
            Id = storeId,
            OwnerIdentityId = userId,
            Name = "Old Name",
            Description = "Old Desc",
            UpdatedAt = DateTime.MinValue,
        };
        _dbContext.Stores.Add(originalStore);
        await _dbContext.SaveChangesAsync();

        SetupUser(userId);

        var command = new UpdateStore.Request(
            Name: "New Super Name",
            Description: "New Description",
            LogoUrl: "http://logo.png",
            CoverImageUrl: null
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value.Should().BeTrue();

        var updatedStore = await _dbContext.Stores.FindAsync(storeId);
        updatedStore.Name.Should().Be("New Super Name");
        updatedStore.Description.Should().Be("New Description");
        updatedStore.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}
