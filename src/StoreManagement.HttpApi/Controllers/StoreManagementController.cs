using StoreManagement.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace StoreManagement.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class StoreManagementController : AbpControllerBase
{
    protected StoreManagementController()
    {
        LocalizationResource = typeof(StoreManagementResource);
    }
}
