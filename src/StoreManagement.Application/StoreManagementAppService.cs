using StoreManagement.Localization;
using Volo.Abp.Application.Services;

namespace StoreManagement;

/* Inherit your application services from this class.
 */
public abstract class StoreManagementAppService : ApplicationService
{
    protected StoreManagementAppService()
    {
        LocalizationResource = typeof(StoreManagementResource);
    }
}
