using Microsoft.AspNetCore.Builder;
using StoreManagement;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();
builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("StoreManagement.Web.csproj"); 
await builder.RunAbpModuleAsync<StoreManagementWebTestModule>(applicationName: "StoreManagement.Web");

public partial class Program
{
}
