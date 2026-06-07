using System;
using StoreManagement.Products;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace StoreManagement.Orders;

public class OrderItem : FullAuditedEntity<Guid>
{
    public Guid OrderId { get; private set; }

    public Order Order { get; private set; } = null!;

    public Guid ProductVariantId { get; private set; }

    public ProductVariant ProductVariant { get; private set; } = null!;

    public string ProductName { get; private set; } = string.Empty;

    public string Color { get; private set; } = string.Empty;

    public string Size { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineTotal { get; private set; }

    protected OrderItem()
    {
    }

    public OrderItem(
        Guid id,
        Guid orderId,
        Guid productVariantId,
        string productName,
        string color,
        string size,
        int quantity,
        decimal unitPrice)
        : base(id)
    {
        if (orderId == Guid.Empty)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.OrderItemRequired);
        }

        if (productVariantId == Guid.Empty)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.OrderProductVariantNotFound);
        }

        OrderId = orderId;
        ProductVariantId = productVariantId;

        SetProductSnapshot(productName, color, size);
        ChangeQuantity(quantity);
        ChangeUnitPrice(unitPrice);
    }

    public void ChangeQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.OrderQuantityInvalid);
        }

        Quantity = quantity;
        RecalculateLineTotal();
    }

    public void ChangeUnitPrice(decimal unitPrice)
    {
        if (unitPrice < 0)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.OrderUnitPriceInvalid);
        }

        UnitPrice = unitPrice;
        RecalculateLineTotal();
    }

    private void SetProductSnapshot(
        string productName,
        string color,
        string size)
    {
        ProductName = string.IsNullOrWhiteSpace(productName)
            ? string.Empty
            : productName.Trim();

        Color = string.IsNullOrWhiteSpace(color)
            ? ProductVariantConsts.NoColor
            : color.Trim();

        Size = string.IsNullOrWhiteSpace(size)
            ? ProductVariantConsts.NoSize
            : size.Trim();
    }

    private void RecalculateLineTotal()
    {
        LineTotal = Quantity * UnitPrice;
    }
}