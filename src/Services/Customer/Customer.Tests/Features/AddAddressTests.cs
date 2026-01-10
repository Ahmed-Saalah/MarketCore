using System.Security.Claims;
using Core.Domain.Errors;
using Customer.API.Data;
using Customer.API.Entities;
using Customer.API.Features;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Customer.Tests.Features;

public class AddAddressTests
{
    private readonly CustomerDbContext _dbContext;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly AddAddress.Handler _handler;

    public AddAddressTests()
    {
        // 1. Setup In-Memory Database
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new CustomerDbContext(options);
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _handler = new AddAddress.Handler(_dbContext, _httpContextAccessorMock.Object);
    }

    private void SetupUserContext(int identityId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, identityId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var context = new DefaultHttpContext { User = claimsPrincipal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserClaimIsMissing()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(new DefaultHttpContext()); // No User
        var request = new AddAddress.Request("Street", "City", "State", "Country", "12345", true);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Error.Should().BeOfType<UnauthorizedError>();
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenCustomerProfileDoesNotExist()
    {
        // Arrange
        SetupUserContext(999);
        var request = new AddAddress.Request("Street", "City", "State", "Country", "12345", true);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Error.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task Handle_ShouldForceIsDefaultTrue_WhenAddingFirstAddress()
    {
        // Arrange
        int userId = 1;
        var customer = new API.Entities.Customer
        {
            Id = Guid.NewGuid(),
            IdentityId = userId,
            Addresses = new List<Address>(),
        };
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        SetupUserContext(userId);

        var request = new AddAddress.Request("Street", "City", "State", "Country", "12345", false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Value.Should().NotBeEmpty();

        var savedAddress = await _dbContext.Addresses.FirstAsync();
        savedAddress.IsDefault.Should().BeTrue();
        savedAddress.CustomerId.Should().Be(customer.Id);
    }

    [Fact]
    public async Task Handle_ShouldUnsetOldDefault_WhenNewAddressIsDefault()
    {
        // Arrange
        int userId = 2;
        var customerId = Guid.NewGuid();

        // Existing Default Address
        var oldAddress = new Address
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            IsDefault = true,
            Street = "Old St",
        };

        var customer = new API.Entities.Customer
        {
            Id = customerId,
            IdentityId = userId,
            Addresses = new List<Address> { oldAddress },
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        SetupUserContext(userId);

        // Add NEW Default Address
        var request = new AddAddress.Request("New St", "City", "State", "Country", "12345", true);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Value.Should().NotBeEmpty();

        var dbAddresses = await _dbContext
            .Addresses.Where(a => a.CustomerId == customerId)
            .ToListAsync();

        dbAddresses.Should().HaveCount(2);

        dbAddresses.First(a => a.Street == "Old St").IsDefault.Should().BeFalse();

        dbAddresses.First(a => a.Street == "New St").IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldKeepOldDefault_WhenNewAddressIsNotDefault()
    {
        // Arrange
        int userId = 3;
        var customerId = Guid.NewGuid();

        var oldAddress = new Address
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            IsDefault = true,
            Street = "Old St",
        };

        var customer = new API.Entities.Customer
        {
            Id = customerId,
            IdentityId = userId,
            Addresses = new List<Address> { oldAddress },
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        SetupUserContext(userId);

        var request = new AddAddress.Request("New St", "City", "State", "Country", "12345", false);

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        var dbOldAddress = await _dbContext.Addresses.FirstAsync(a => a.Street == "Old St");
        var dbNewAddress = await _dbContext.Addresses.FirstAsync(a => a.Street == "New St");

        dbOldAddress.IsDefault.Should().BeTrue();
        dbNewAddress.IsDefault.Should().BeFalse();
    }
}
