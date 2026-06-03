using Volo.Abp.Modularity;

namespace StoreManagement;

/* Inherit from this class for your domain layer tests. */
public abstract class StoreManagementDomainTestBase<TStartupModule> : StoreManagementTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
