using Volo.Abp.Modularity;

namespace StoreManagement;

[DependsOn(
    typeof(StoreManagementApplicationModule),
    typeof(StoreManagementDomainTestModule)
)]
public class StoreManagementApplicationTestModule : AbpModule
{

}
