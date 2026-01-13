using System.Security.Claims;
using Auth.API.Data;
using Auth.API.Models;
using Auth.API.Services;
using Core.Domain.Errors;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Auth.Tests.Features;

public class RefreshTokenTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly AuthDbContext _dbContext;
    private readonly API.Features.RefreshToken.Handler _handler;

    public RefreshTokenTests()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AuthDbContext(options);

        var store = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            store.Object,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null
        );

        _tokenServiceMock = new Mock<ITokenService>();

        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        _handler = new API.Features.RefreshToken.Handler(
            _dbContext,
            _userManagerMock.Object,
            _tokenServiceMock.Object,
            _httpContextAccessorMock.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenTokenIsValid()
    {
        // Arrange
        var oldTokenString = "valid-old-token";
        var user = new User
        {
            Id = 1,
            UserName = "testuser",
            RefreshTokens = new List<API.Models.RefreshToken>(),
        };

        var existingToken = new API.Models.RefreshToken
        {
            Token = oldTokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            User = user,
            UserId = user.Id,
        };
        _dbContext.Users.Add(user);
        _dbContext.RefreshTokens.Add(existingToken);
        await _dbContext.SaveChangesAsync();

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Customer" });

        _userManagerMock.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());

        var newAccessToken = "new-access-token-123";
        var newRefreshTokenObj = new API.Models.RefreshToken { Token = "new-refresh-token-xyz" };

        _tokenServiceMock
            .Setup(x =>
                x.GenerateAccessToken(user, It.IsAny<IList<string>>(), It.IsAny<IList<Claim>>())
            )
            .Returns(newAccessToken);

        _tokenServiceMock
            .Setup(x => x.GenerateRefreshToken(It.IsAny<string>()))
            .Returns(newRefreshTokenObj);

        // Act
        var request = new API.Features.RefreshToken.Request(oldTokenString);
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value.AccessToken.Should().Be(newAccessToken);
        result.Value.RefreshToken.Should().Be(newRefreshTokenObj.Token);

        var oldTokenInDb = await _dbContext.RefreshTokens.FirstAsync(t =>
            t.Token == oldTokenString
        );
        oldTokenInDb.RevokedAt.Should().NotBeNull();
        oldTokenInDb.ReplacedByToken.Should().Be(newRefreshTokenObj.Token);

        var newTokenInDb = await _dbContext.RefreshTokens.FirstOrDefaultAsync(t =>
            t.Token == newRefreshTokenObj.Token
        );
        newTokenInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenTokenDoesNotExist()
    {
        var request = new API.Features.RefreshToken.Request("non-existent-token");
        var result = await _handler.Handle(request, CancellationToken.None);

        result.Value.Should().BeNull();
        result.Error.Should().BeOfType<UnauthorizedError>();
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenTokenIsAlreadyRevoked()
    {
        var revokedTokenStr = "revoked-token";
        var user = new User { Id = 2 };

        var revokedToken = new API.Models.RefreshToken
        {
            Token = revokedTokenStr,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = DateTime.UtcNow.AddMinutes(-5),
            User = user,
        };

        _dbContext.RefreshTokens.Add(revokedToken);
        await _dbContext.SaveChangesAsync();

        var request = new API.Features.RefreshToken.Request(revokedTokenStr);
        var result = await _handler.Handle(request, CancellationToken.None);

        result.Value.Should().BeNull();
        result.Error.Message.Should().Be("Token is expired or already revoked.");
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenTokenIsExpired()
    {
        var expiredTokenStr = "expired-token";
        var user = new User { Id = 3 };

        var expiredToken = new API.Models.RefreshToken
        {
            Token = expiredTokenStr,
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            User = user,
        };

        _dbContext.RefreshTokens.Add(expiredToken);
        await _dbContext.SaveChangesAsync();

        var request = new API.Features.RefreshToken.Request(expiredTokenStr);
        var result = await _handler.Handle(request, CancellationToken.None);

        result.Value.Should().BeNull();
        result.Error.Message.Should().Be("Token is expired or already revoked.");
    }
}
