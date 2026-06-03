using System;
using System.Collections.Generic;
using StoreManagement.Categories;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace StoreManagement.Products;

public class Product : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public decimal Price { get; private set; }

    public bool IsActive { get; private set; }

    public ProductTargetAudience TargetAudience { get; private set; }

    public Guid CategoryId { get; private set; }

    public Category Category { get; private set; } = null!;

    public ICollection<ProductVariant> Variants { get; private set; } = new List<ProductVariant>();

    protected Product()
    {
    }

    public Product(
        Guid id,
        string name,
        string? description,
        decimal price,
        Guid categoryId,
        ProductTargetAudience targetAudience = ProductTargetAudience.Unisex,
        bool isActive = true)
        : base(id)
    {
        SetName(name);
        SetDescription(description);
        SetPrice(price);
        CategoryId = categoryId;
        TargetAudience = targetAudience;
        IsActive = isActive;
    }

    public void Rename(string name)
    {
        SetName(name);
    }

    public void SetDescription(string? description)
    {
        Description = NormalizeOptionalText(
            description,
            nameof(Description),
            ProductConsts.MaxDescriptionLength);
    }

    public void SetPrice(decimal price)
    {
        if (price < ProductConsts.MinPrice)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductPriceInvalid);
        }

        Price = price;
    }

    public void ChangeCategory(Guid categoryId)
    {
        CategoryId = categoryId;
    }

    public void ChangeTargetAudience(ProductTargetAudience targetAudience)
    {
        TargetAudience = targetAudience;
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

    private void SetName(string name)
    {
        Name = NormalizeRequiredText(
            name,
            nameof(Name),
            ProductConsts.MaxNameLength);

        NormalizedName = Name.ToUpperInvariant();
    }

    private static string NormalizeRequiredText(
        string value,
        string propertyName,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductNameRequired)
                .WithData("PropertyName", propertyName);
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maxLength)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductTextTooLong)
                .WithData("PropertyName", propertyName)
                .WithData("MaxLength", maxLength);
        }

        return normalizedValue;
    }

    private static string? NormalizeOptionalText(
        string? value,
        string propertyName,
        int maxLength)
    {
        var normalizedValue = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            return null;
        }

        if (normalizedValue.Length > maxLength)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.ProductTextTooLong)
                .WithData("PropertyName", propertyName)
                .WithData("MaxLength", maxLength);
        }

        return normalizedValue;
    }
}