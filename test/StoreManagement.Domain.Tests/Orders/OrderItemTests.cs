using System;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace StoreManagement.Orders;

/* Plain unit tests on the OrderItem entity. No DI/DB is needed here since
 * SetCostPriceSnapshot is a pure in-memory guard, so this does not inherit
 * StoreManagementDomainTestBase. */
public class OrderItemTests
{
    private static OrderItem CreateItem()
    {
        return new OrderItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Product",
            "Red",
            "M",
            1,
            100m
        );
    }

    [Fact]
    public void SetCostPriceSnapshot_Should_Throw_When_Negative()
    {
        var item = CreateItem();

        Should.Throw<BusinessException>(() =>
            item.SetCostPriceSnapshot(-1m)
        );
    }

    [Fact]
    public void SetCostPriceSnapshot_Should_Accept_Null()
    {
        var item = CreateItem();

        item.SetCostPriceSnapshot(null);

        item.CostPriceAtSale.ShouldBeNull();
    }

    [Fact]
    public void SetCostPriceSnapshot_Should_Accept_Valid_Value()
    {
        var item = CreateItem();

        item.SetCostPriceSnapshot(50m);

        item.CostPriceAtSale.ShouldBe(50m);
    }
}
