using System.Security.Claims;
using Auth.API.Features;
using Auth.API.Messages;
using Auth.API.Models;
using Auth.API.Services;
using Core.Domain.Errors;
using Core.Messaging;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Auth.Tests.Features;

public class CreateUserTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IValidator<CreateUser.Request>> _validatorMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

    private readonly CreateUser.Handler _handler;

    public CreateUserTests()
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
        _validatorMock = new Mock<IValidator<CreateUser.Request>>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        _handler = new CreateUser.Handler(
            _userManagerMock.Object,
            _tokenServiceMock.Object,
            _validatorMock.Object,
            _eventPublisherMock.Object,
            _httpContextAccessorMock.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateUser.Request(
            "testuser",
            "test@mail.com",
            "+1234567890",
            "Password123!",
            "Test User",
            "Customer",
            null
        );

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<User>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.AddToRoleAsync(It.IsAny<User>(), request.Role))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        var expectedToken = "access-token-123";
        var expectedRefreshToken = new API.Models.RefreshToken { Token = "refresh-token-xyz" };

        _tokenServiceMock
            .Setup(x =>
                x.GenerateAccessToken(
                    It.IsAny<User>(),
                    It.IsAny<IList<string>>(),
                    It.IsAny<IList<Claim>>()
                )
            )
            .Returns(expectedToken);

        _tokenServiceMock
            .Setup(x => x.GenerateRefreshToken(It.IsAny<string>()))
            .Returns(expectedRefreshToken);

        // Act
        var response = await _handler.Handle(request, CancellationToken.None);

        // Assert
        response.Error.Should().BeNull();

        response.Value.AccessToken.Should().Be(expectedToken);
        response.Value.RefreshToken.Should().Be(expectedRefreshToken.Token);

        _eventPublisherMock.Verify(
            x =>
                x.PublishAsync(
                    It.Is<UserCreatedEvent>(e =>
                        e.Email == request.Email && e.Role == request.Role
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenValidatorFails()
    {
        // Arrange
        var request = new CreateUser.Request("short", "bad-email", "", "pass", "", "Admin", null);

        var validationFailure = new ValidationFailure("Email", "Invalid email");
        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { validationFailure }));

        // Act
        var response = await _handler.Handle(request, CancellationToken.None);

        // Assert
        response.Value.Should().BeNull();
        response.Error.Should().BeOfType<ValidationError>();

        _userManagerMock.Verify(
            x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenIdentityCreateFails()
    {
        // Arrange
        var request = new CreateUser.Request(
            "user",
            "mail@test.com",
            "123",
            "pass",
            "Name",
            "Role",
            null
        );

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var identityError = new IdentityError
        {
            Code = "PasswordTooShort",
            Description = "Password too short",
        };
        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<User>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        // Act
        var response = await _handler.Handle(request, CancellationToken.None);

        // Assert
        response.Value.Should().BeNull();
        response.Error.Should().BeOfType<ValidationError>();

        _tokenServiceMock.Verify(
            x =>
                x.GenerateAccessToken(
                    It.IsAny<User>(),
                    It.IsAny<IList<string>>(),
                    It.IsAny<IList<Claim>>()
                ),
            Times.Never
        );
    }
}
