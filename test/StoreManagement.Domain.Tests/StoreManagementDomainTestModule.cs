using Volo.Abp.Modularity;

namespace StoreManagement;

[DependsOn(
    typeof(StoreManagementDomainModule),
    typeof(StoreManagementTestBaseModule)
)]
public class StoreManagementDomainTestModule : AbpModule
{

}
