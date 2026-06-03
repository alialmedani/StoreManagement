using StoreManagement.Samples;
using Xunit;

namespace StoreManagement.EntityFrameworkCore.Applications;

[Collection(StoreManagementTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<StoreManagementEntityFrameworkCoreTestModule>
{

}
