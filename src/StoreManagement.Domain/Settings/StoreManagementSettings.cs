namespace StoreManagement.Settings;

public static class StoreManagementSettings
{
    private const string Prefix = "StoreManagement";

    public const string AllowNegativeStock = Prefix + ".Inventory.AllowNegativeStock";

    public const string LowStockThreshold = Prefix + ".Inventory.LowStockThreshold";

    public const string AllowCancelConfirmedOrder = Prefix + ".Orders.AllowCancelConfirmedOrder";

    public const string OrderNumberPrefix = Prefix + ".Orders.NumberPrefix";
}