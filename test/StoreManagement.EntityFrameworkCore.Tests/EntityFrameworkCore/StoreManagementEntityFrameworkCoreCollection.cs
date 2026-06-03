using Xunit;

namespace StoreManagement.EntityFrameworkCore;

[CollectionDefinition(StoreManagementTestConsts.CollectionDefinitionName)]
public class StoreManagementEntityFrameworkCoreCollection : ICollectionFixture<StoreManagementEntityFrameworkCoreFixture>
{

}
