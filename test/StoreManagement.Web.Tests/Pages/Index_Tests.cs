using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace StoreManagement.Pages;

[Collection(StoreManagementTestConsts.CollectionDefinitionName)]
public class Index_Tests : StoreManagementWebTestBase
{
    [Fact]
    public async Task Welcome_Page()
    {
        var response = await GetResponseAsStringAsync("/");
        response.ShouldNotBeNull();
    }
}
