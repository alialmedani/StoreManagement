namespace StoreManagement;

public static class StoreManagementDomainErrorCodes
{
    public const string CategoryNameRequired = "StoreManagement:CategoryNameRequired";
    public const string CategoryTextTooLong = "StoreManagement:CategoryTextTooLong";
    public const string CategoryNameAlreadyExists = "StoreManagement:CategoryNameAlreadyExists";
    public const string CategoryNotDeleted = "StoreManagement:CategoryNotDeleted";
    public const string ProductNameRequired = "StoreManagement:ProductNameRequired";
    public const string ProductTextTooLong = "StoreManagement:ProductTextTooLong";
    public const string ProductPriceInvalid = "StoreManagement:ProductPriceInvalid";
    public const string ProductNameAlreadyExists = "StoreManagement:ProductNameAlreadyExists";
    public const string ProductCategoryNotFound = "StoreManagement:ProductCategoryNotFound";
    public const string ProductCategoryCannotBeChanged = "StoreManagement:ProductCategoryCannotBeChanged";

    public const string ProductVariantColorRequired = "StoreManagement:ProductVariantColorRequired";
    public const string ProductVariantSizeRequired = "StoreManagement:ProductVariantSizeRequired";
    public const string ProductVariantTextTooLong = "StoreManagement:ProductVariantTextTooLong";
    public const string ProductVariantStockCannotBeNegative = "StoreManagement:ProductVariantStockCannotBeNegative";
}
