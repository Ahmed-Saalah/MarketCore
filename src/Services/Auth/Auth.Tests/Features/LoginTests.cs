using System.Security.Claims;
using Auth.API.Constants;
using Auth.API.Data;
using Auth.API.Features;
using Auth.API.Models;
using Auth.API.Services;
using Core.Domain.Errors;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Auth.Tests.Features;

public class LoginTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly AuthDbContext _dbContext;

    private readonly Login.Handler _handler;

    public LoginTests()
    {
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
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AuthDbContext(options);

        _handler = new Login.Handler(
            _dbContext,
            _tokenServiceMock.Object,
            _httpContextAccessorMock.Object,
            _userManagerMock.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenCredentialsAreValid()
    {
        var request = new Login.Request("test@mail.com", "Password123!");
        var user = new User
        {
            Id = 1,
            Email = request.Email,
            UserName = "testuser",
            DisplayName = "Test User",
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { Roles.Customer });

        _userManagerMock.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());

        var expectedAccessToken = "access-token-abc";
        var expectedRefreshToken = new API.Models.RefreshToken { Token = "refresh-token-xyz" };

        _tokenServiceMock
            .Setup(x =>
                x.GenerateAccessToken(user, It.IsAny<IList<string>>(), It.IsAny<IList<Claim>>())
            )
            .Returns(expectedAccessToken);

        _tokenServiceMock
            .Setup(x => x.GenerateRefreshToken(It.IsAny<string>()))
            .Returns(expectedRefreshToken);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Value.Should().NotBeNull();
        var response = result.Value;

        response.AccessToken.Should().Be(expectedAccessToken);
        response.RefreshToken.Should().Be(expectedRefreshToken.Token);
        response.Role.Should().Be(Roles.Customer);
        response.Profile.Email.Should().Be(request.Email);

        _dbContext.Entry(user).Collection(u => u.RefreshTokens).IsLoaded.Should().BeFalse();
        user.RefreshTokens.Should().Contain(expectedRefreshToken);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserNotFound()
    {
        // Arrange
        var request = new Login.Request("unknown@mail.com", "Password123!");

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Error.Should().BeOfType<UnauthorizedError>();
        result.Error.Message.Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenPasswordIsInvalid()
    {
        // Arrange
        var request = new Login.Request("test@mail.com", "WrongPassword!");
        var user = new User { Email = request.Email };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Value.Should().BeNull();
        result.Error.Should().BeOfType<UnauthorizedError>();
    }

    [Fact]
    public void Validator_ShouldHaveError_WhenEmailIsEmpty()
    {
        var validator = new Login.Validator();
        var request = new Login.Request("", "pass");

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}
