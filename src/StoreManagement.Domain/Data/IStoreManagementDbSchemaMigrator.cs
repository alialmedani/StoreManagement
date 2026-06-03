using System.Threading.Tasks;

namespace StoreManagement.Data;

public interface IStoreManagementDbSchemaMigrator
{
    Task MigrateAsync();
}
