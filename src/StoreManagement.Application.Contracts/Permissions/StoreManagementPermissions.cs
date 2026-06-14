namespace StoreManagement.Permissions;

public static class StoreManagementPermissions
{
    public const string GroupName = "StoreManagement";

    public static class Categories
    {
        public const string Default = GroupName + ".Categories";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string Restore = Default + ".Restore";
    }

    public static class Products
    {
        public const string Default = GroupName + ".Products";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string Restore = Default + ".Restore";
    }

    public static class ProductVariants
    {
        public const string Default = GroupName + ".ProductVariants";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string Restore = Default + ".Restore";
    }

    public static class Inventory
    {
        public const string Default = GroupName + ".Inventory";
        public const string AdjustStock = Default + ".AdjustStock";
    }

    public static class Orders
    {
        public const string Default = GroupName + ".Orders";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string Confirm = Default + ".Confirm";
        public const string Cancel = Default + ".Cancel";
        public const string RecordPayment = Default + ".RecordPayment";
    }
}