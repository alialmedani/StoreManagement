using StoreManagement.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace StoreManagement.Permissions;

public class StoreManagementPermissionDefinitionProvider
    : PermissionDefinitionProvider
{
    public override void Define(
        IPermissionDefinitionContext context)
    {
        var storeManagementGroup = context.AddGroup(
            StoreManagementPermissions.GroupName,
            L("Permission:StoreManagement")
        );

        var categoriesPermission =
            storeManagementGroup.AddPermission(
                StoreManagementPermissions.Categories.Default,
                L("Permission:Categories")
            );

        categoriesPermission.AddChild(
            StoreManagementPermissions.Categories.Create,
            L("Permission:Categories.Create")
        );

        categoriesPermission.AddChild(
            StoreManagementPermissions.Categories.Edit,
            L("Permission:Categories.Edit")
        );

        categoriesPermission.AddChild(
            StoreManagementPermissions.Categories.Delete,
            L("Permission:Categories.Delete")
        );

        categoriesPermission.AddChild(
            StoreManagementPermissions.Categories.Restore,
            L("Permission:Categories.Restore")
        );

        var productsPermission =
            storeManagementGroup.AddPermission(
                StoreManagementPermissions.Products.Default,
                L("Permission:Products")
            );

        productsPermission.AddChild(
            StoreManagementPermissions.Products.Create,
            L("Permission:Products.Create")
        );

        productsPermission.AddChild(
            StoreManagementPermissions.Products.Edit,
            L("Permission:Products.Edit")
        );

        productsPermission.AddChild(
            StoreManagementPermissions.Products.Delete,
            L("Permission:Products.Delete")
        );

        productsPermission.AddChild(
            StoreManagementPermissions.Products.Restore,
            L("Permission:Products.Restore")
        );

        var productVariantsPermission =
            storeManagementGroup.AddPermission(
                StoreManagementPermissions.ProductVariants.Default,
                L("Permission:ProductVariants")
            );

        productVariantsPermission.AddChild(
            StoreManagementPermissions.ProductVariants.Create,
            L("Permission:ProductVariants.Create")
        );

        productVariantsPermission.AddChild(
            StoreManagementPermissions.ProductVariants.Edit,
            L("Permission:ProductVariants.Edit")
        );

        productVariantsPermission.AddChild(
            StoreManagementPermissions.ProductVariants.Delete,
            L("Permission:ProductVariants.Delete")
        );

        productVariantsPermission.AddChild(
            StoreManagementPermissions.ProductVariants.Restore,
            L("Permission:ProductVariants.Restore")
        );

        var inventoryPermission =
            storeManagementGroup.AddPermission(
                StoreManagementPermissions.Inventory.Default,
                L("Permission:Inventory")
            );

        inventoryPermission.AddChild(
            StoreManagementPermissions.Inventory.AdjustStock,
            L("Permission:Inventory.AdjustStock")
        );

        var ordersPermission =
            storeManagementGroup.AddPermission(
                StoreManagementPermissions.Orders.Default,
                L("Permission:Orders")
            );

        ordersPermission.AddChild(
            StoreManagementPermissions.Orders.Create,
            L("Permission:Orders.Create")
        );

        ordersPermission.AddChild(
            StoreManagementPermissions.Orders.Edit,
            L("Permission:Orders.Edit")
        );

        ordersPermission.AddChild(
            StoreManagementPermissions.Orders.Delete,
            L("Permission:Orders.Delete")
        );

        ordersPermission.AddChild(
            StoreManagementPermissions.Orders.Confirm,
            L("Permission:Orders.Confirm")
        );

        ordersPermission.AddChild(
            StoreManagementPermissions.Orders.Cancel,
            L("Permission:Orders.Cancel")
        );

        ordersPermission.AddChild(
            StoreManagementPermissions.Orders.RecordPayment,
            L("Permission:Orders.RecordPayment")
        );
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString
            .Create<StoreManagementResource>(name);
    }
}