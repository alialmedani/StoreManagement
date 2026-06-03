using Volo.Abp.Modularity;

namespace StoreManagement;

public abstract class StoreManagementApplicationTestBase<TStartupModule> : StoreManagementTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
