using StoreManagement.Localization;
using Volo.Abp.Localization;
using Volo.Abp.Settings;

namespace StoreManagement.Settings;

public class StoreManagementSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(
            new SettingDefinition(
                StoreManagementSettings.AllowNegativeStock,
                "false",
                L("DisplayName:AllowNegativeStock")
            )
        );

        context.Add(
            new SettingDefinition(
                StoreManagementSettings.AllowCancelConfirmedOrder,
                "true",
                L("DisplayName:AllowCancelConfirmedOrder")
            )
        );

        context.Add(
            new SettingDefinition(
                StoreManagementSettings.OrderNumberPrefix,
                "ORD",
                L("DisplayName:OrderNumberPrefix")
            )
        );
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<StoreManagementResource>(name);
    }
}