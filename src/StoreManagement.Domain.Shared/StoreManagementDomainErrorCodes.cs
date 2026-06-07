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
    public const string CategoryHasProducts = "StoreManagement:CategoryHasProducts";
    public const string CategorySizeTypeCannotBeChanged = "StoreManagement:CategorySizeTypeCannotBeChanged";
    
    public const string ProductVariantAlreadyExists = "StoreManagement:ProductVariantAlreadyExists";
    public const string ProductVariantNotFound = "StoreManagement:ProductVariantNotFound";
    public const string ProductVariantProductNotFound = "StoreManagement:ProductVariantProductNotFound";
    public const string ProductVariantInvalidSizeForCategory = "StoreManagement:ProductVariantInvalidSizeForCategory";
    public const string ProductVariantHasStock = "StoreManagement:ProductVariantHasStock";
    public const string ProductVariantCannotRestoreDuplicate = "StoreManagement:ProductVariantCannotRestoreDuplicate";
    public const string ProductHasVariants = "StoreManagement:ProductHasVariants";
    
    public const string InventoryProductVariantNotFound = "StoreManagement:InventoryProductVariantNotFound";
    public const string InventoryQuantityChangeCannotBeZero = "StoreManagement:InventoryQuantityChangeCannotBeZero";
    public const string InventoryStockCannotBeNegative = "StoreManagement:InventoryStockCannotBeNegative";
    public const string InventoryNoteTooLong = "StoreManagement:InventoryNoteTooLong";
    public const string ProductVariantHasMovements = "StoreManagement:ProductVariantHasMovements";
    public const string InventoryInvalidMovementSource = "StoreManagement:InventoryInvalidMovementSource";
    public const string InventoryManualMovementTypeNotAllowed = "StoreManagement:InventoryManualMovementTypeNotAllowed";
    
    public const string OrderNumberRequired = "StoreManagement:OrderNumberRequired";
    public const string OrderTextTooLong = "StoreManagement:OrderTextTooLong";
    public const string OrderItemRequired = "StoreManagement:OrderItemRequired";
    public const string OrderQuantityInvalid = "StoreManagement:OrderQuantityInvalid";
    public const string OrderUnitPriceInvalid = "StoreManagement:OrderUnitPriceInvalid";
    public const string OrderCannotBeConfirmed = "StoreManagement:OrderCannotBeConfirmed";
    public const string OrderCannotBeCancelled = "StoreManagement:OrderCannotBeCancelled";
    public const string OrderCannotBeUpdated = "StoreManagement:OrderCannotBeUpdated";
    public const string OrderProductVariantNotFound = "StoreManagement:OrderProductVariantNotFound";
    public const string OrderProductVariantInactive = "StoreManagement:OrderProductVariantInactive";
    public const string OrderInsufficientStock = "StoreManagement:OrderInsufficientStock";
    public const string OrderCustomerNameRequired = "StoreManagement:OrderCustomerNameRequired";
    public const string OrderCannotBeDeleted = "StoreManagement:OrderCannotBeDeleted";
    
}
