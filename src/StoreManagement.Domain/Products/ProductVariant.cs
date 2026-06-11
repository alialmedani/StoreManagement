using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace StoreManagement.Products;

public class ProductVariant : FullAuditedEntity<Guid>
{
    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public string Color { get; private set; } = string.Empty;

    public string NormalizedColor { get; private set; } = string.Empty;

    public string Size { get; private set; } = string.Empty;

    public string NormalizedSize { get; private set; } = string.Empty;

    public int StockQuantity { get; private set; }

    public bool IsActive { get; private set; }

    protected ProductVariant()
    {
    }

    public ProductVariant(
        Guid id,
        Guid productId,
        string color,
        string size,
        int stockQuantity,
        bool isActive = true)
        : base(id)
    {
        ProductId = productId;
        SetColorAndSize(color, size);
        SetStockQuantity(stockQuantity, allowNegativeStock: false);
        IsActive = isActive;
    }

    public void SetColorAndSize(string color, string size)
    {
        Color = NormalizeRequiredText(
            color,
            nameof(Color),
            ProductVariantConsts.MaxColorLength,
            StoreManagementDomainErrorCodes.ProductVariantColorRequired);

        Size = NormalizeRequiredText(
            size,
            nameof(Size),
            ProductVariantConsts.MaxSizeLength,
            StoreManagementDomainErrorCodes.ProductVariantSizeRequired);

        NormalizedColor = Color.ToUpperInvariant();
        NormalizedSize = Size.ToUpperInvariant();
    }

    public void SetStockQuantity(int stockQuantity)
    {
        SetStockQuantity(stockQuantity, allowNegativeStock: false);
    }

    public void SetStockQuantity(int stockQuantity, bool allowNegativeStock)
    {
        if (!allowNegativeStock && stockQuantity < 0)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantStockCannotBeNegative);
        }

        StockQuantity = stockQuantity;
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantStockCannotBeNegative);
        }

        StockQuantity += quantity;
    }

    public void DecreaseStock(int quantity)
    {
        DecreaseStock(quantity, allowNegativeStock: false);
    }

    public void DecreaseStock(int quantity, bool allowNegativeStock)
    {
        if (quantity <= 0)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantStockCannotBeNegative);
        }

        var newQuantity = StockQuantity - quantity;

        if (!allowNegativeStock && newQuantity < 0)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantStockCannotBeNegative);
        }

        StockQuantity = newQuantity;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletionTime = null;
        DeleterId = null;
    }

    private static string NormalizeRequiredText(
        string value,
        string propertyName,
        int maxLength,
        string requiredErrorCode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessException(requiredErrorCode)
                .WithData("PropertyName", propertyName);
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maxLength)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductVariantTextTooLong)
                .WithData("PropertyName", propertyName)
                .WithData("MaxLength", maxLength);
        }

        return normalizedValue;
    }
}