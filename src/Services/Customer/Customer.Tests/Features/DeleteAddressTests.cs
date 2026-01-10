using System.Security.Claims;
using Core.Domain.Errors;
using Customer.API.Data;
using Customer.API.Entities;
using Customer.API.Features;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Customer.Tests.Features;

public class DeleteAddressTests
{
    private readonly CustomerDbContext _dbContext;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly DeleteAddress.Handler _handler;

    public DeleteAddressTests()
    {
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new CustomerDbContext(options);
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _handler = new DeleteAddress.Handler(_dbContext, _httpContextAccessorMock.Object);
    }

    private void SetupUserContext(int identityId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, identityId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenAddressBelongsToUser()
    {
        // Arrange
        int userId = 10;
        var addressId = Guid.NewGuid();

        var customer = new API.Entities.Customer
        {
            Id = Guid.NewGuid(),
            IdentityId = userId,
            Addresses = new List<Address>
            {
                new Address { Id = addressId, Street = "123 Main St" },
            },
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        SetupUserContext(userId);

        // Act
        var result = await _handler.Handle(
            new DeleteAddress.Request(addressId),
            CancellationToken.None
        );

        // Assert
        result.Value.Should().BeTrue();

        var addressInDb = await _dbContext.Addresses.FindAsync(addressId);
        addressInDb.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenAddressDoesNotBelongToUser()
    {
        // Arrange
        int userIdA = 10;
        int userIdB = 20;
        var targetAddressId = Guid.NewGuid();

        var customerA = new API.Entities.Customer
        {
            Id = Guid.NewGuid(),
            IdentityId = userIdA,
            Addresses = new List<Address>
            {
                new Address { Id = targetAddressId, Street = "Secret St" },
            },
        };

        var customerB = new API.Entities.Customer
        {
            Id = Guid.NewGuid(),
            IdentityId = userIdB,
            Addresses = new List<Address>(),
        };

        _dbContext.Customers.AddRange(customerA, customerB);
        await _dbContext.SaveChangesAsync();
        SetupUserContext(userIdB);

        var result = await _handler.Handle(
            new DeleteAddress.Request(targetAddressId),
            CancellationToken.None
        );

        // Assert
        result.Error.Should().BeOfType<NotFound>();
        var addressInDb = await _dbContext.Addresses.FindAsync(targetAddressId);
        addressInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenAddressIdDoesNotExist()
    {
        // Arrange
        int userId = 5;
        var customer = new API.Entities.Customer
        {
            Id = Guid.NewGuid(),
            IdentityId = userId,
            Addresses = new List<Address>(),
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        SetupUserContext(userId);

        // Act
        var result = await _handler.Handle(
            new DeleteAddress.Request(Guid.NewGuid()),
            CancellationToken.None
        );

        // Assert
        result.Error.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIsNotLoggedIn()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());

        // Act
        var result = await _handler.Handle(
            new DeleteAddress.Request(Guid.NewGuid()),
            CancellationToken.None
        );

        // Assert
        result.Error.Should().BeOfType<UnauthorizedError>();
    }
}
