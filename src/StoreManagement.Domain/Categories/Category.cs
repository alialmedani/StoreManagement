using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace StoreManagement.Categories;

public class Category : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public CategorySizeType SizeType { get; private set; }

    public string? ImageUrl { get; private set; }

    public bool IsActive { get; private set; }

    protected Category()
    {
    }

    public Category(
        Guid id,
        string name,
        string? description,
        CategorySizeType sizeType,
        bool isActive = true)
        : base(id)
    {
        SetName(name);
        SetDescription(description);
        SizeType = sizeType;
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
            CategoryConsts.MaxDescriptionLength);
    }

    public void ChangeSizeType(CategorySizeType sizeType)
    {
        SizeType = sizeType;
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

    public void SetImageUrl(string? imageUrl)
    {
        ImageUrl = imageUrl?.Trim();
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
            CategoryConsts.MaxNameLength);

        NormalizedName = Name.ToUpperInvariant();
    }

    private static string NormalizeRequiredText(
        string value,
        string propertyName,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.CategoryNameRequired)
                .WithData("PropertyName", propertyName);
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maxLength)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.CategoryTextTooLong)
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
            throw new BusinessException(StoreManagementDomainErrorCodes.CategoryTextTooLong)
                .WithData("PropertyName", propertyName)
                .WithData("MaxLength", maxLength);
        }

        return normalizedValue;
    }
}