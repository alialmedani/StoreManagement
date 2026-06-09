using StoreManagement.Localization;
using Volo.Abp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using StoreManagement.Permissions;
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
