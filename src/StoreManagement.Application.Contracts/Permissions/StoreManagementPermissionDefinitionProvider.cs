using StoreManagement.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace StoreManagement.Permissions;

public class StoreManagementPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(StoreManagementPermissions.GroupName);

        //Define your own permissions here. Example:
        //myGroup.AddPermission(StoreManagementPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<StoreManagementResource>(name);
    }
}
