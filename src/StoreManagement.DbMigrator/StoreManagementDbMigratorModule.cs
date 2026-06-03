using StoreManagement.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace StoreManagement.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(StoreManagementEntityFrameworkCoreModule),
    typeof(StoreManagementApplicationContractsModule)
)]
public class StoreManagementDbMigratorModule : AbpModule
{
}
