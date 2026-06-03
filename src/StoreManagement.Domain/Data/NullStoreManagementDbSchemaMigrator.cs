using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace StoreManagement.Data;

/* This is used if database provider does't define
 * IStoreManagementDbSchemaMigrator implementation.
 */
public class NullStoreManagementDbSchemaMigrator : IStoreManagementDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
