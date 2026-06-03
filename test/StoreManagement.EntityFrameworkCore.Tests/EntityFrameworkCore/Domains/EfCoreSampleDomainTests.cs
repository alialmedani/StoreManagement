using StoreManagement.Samples;
using Xunit;

namespace StoreManagement.EntityFrameworkCore.Domains;

[Collection(StoreManagementTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<StoreManagementEntityFrameworkCoreTestModule>
{

}
