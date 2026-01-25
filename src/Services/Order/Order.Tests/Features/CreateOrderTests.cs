using FluentValidation.TestHelper;
using Order.API.Features;

namespace Order.Tests.Features;

public class CreateOrderValidatorTests
{
    private readonly CreateOrder.Validator _validator = new();

    [Fact]
    public void Should_Have_Error_When_StoreId_Is_Empty()
    {
        var request = CreateRequest(storeId: Guid.Empty);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.StoreId);
    }

    [Fact]
    public void Should_Have_Error_When_UserId_Is_Empty()
    {
        var request = CreateRequest(userId: Guid.Empty);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Should_Have_Error_When_Items_List_Is_Empty()
    {
        var request = new CreateOrder.Request(Guid.NewGuid(), Guid.NewGuid(), []);
        var result = _validator.TestValidate(request);
        result
            .ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("Order must contain at least one item.");
    }

    [Fact]
    public void Should_Have_Error_When_Item_ProductId_Is_Empty()
    {
        var items = new List<CreateOrder.CreateOrderItemDto>
        {
            new(Guid.Empty, "Product A", "SKU-A", 10m, 1),
        };
        var request = new CreateOrder.Request(Guid.NewGuid(), Guid.NewGuid(), items);

        var result = _validator.TestValidate(request);

        // FluentValidation uses indexed paths for collections
        result.ShouldHaveValidationErrorFor("Items[0].ProductId");
    }

    [Fact]
    public void Should_Have_Error_When_Item_Quantity_Is_Zero_Or_Negative()
    {
        var items = new List<CreateOrder.CreateOrderItemDto>
        {
            new(Guid.NewGuid(), "Product A", "SKU-A", 10m, 0),
        };
        var request = new CreateOrder.Request(Guid.NewGuid(), Guid.NewGuid(), items);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Items[0].Quantity");
    }

    [Fact]
    public void Should_Have_Error_When_Item_UnitPrice_Is_Negative()
    {
        var items = new List<CreateOrder.CreateOrderItemDto>
        {
            new(Guid.NewGuid(), "Product A", "SKU-A", -5m, 1),
        };
        var request = new CreateOrder.Request(Guid.NewGuid(), Guid.NewGuid(), items);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Items[0].UnitPrice");
    }

    [Fact]
    public void Should_Pass_When_Request_Is_Valid()
    {
        var request = CreateRequest();
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    // Helper to create a valid request base
    private static CreateOrder.Request CreateRequest(Guid? userId = null, Guid? storeId = null)
    {
        return new CreateOrder.Request(
            userId ?? Guid.NewGuid(),
            storeId ?? Guid.NewGuid(),
            [new(Guid.NewGuid(), "Test Product", "SKU-123", 100m, 1)]
        );
    }
}
