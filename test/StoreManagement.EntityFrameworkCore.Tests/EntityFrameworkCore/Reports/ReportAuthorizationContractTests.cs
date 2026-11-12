using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Shouldly;
using StoreManagement.Permissions;
using StoreManagement.Reports;
using Xunit;

namespace StoreManagement.EntityFrameworkCore.Reports;

/// <summary>
/// The test host registers <c>AddAlwaysAllowAuthorization()</c>, so a real
/// permission-denied path cannot be exercised at this layer (that is covered by the
/// authorization interceptor end-to-end, normally asserted at the HttpApi/Web layer).
/// These reflection checks instead pin the authorization CONTRACT: every report
/// endpoint is attribute-protected with the expected permission constant, so an
/// accidental removal or wrong permission is caught here.
/// </summary>
public class ReportAuthorizationContractTests
{
    [Fact]
    public void ReportAppService_Is_Authorize_Protected_At_Class_Level()
    {
        typeof(ReportAppService)
            .GetCustomAttribute<AuthorizeAttribute>()
            .ShouldNotBeNull();
    }

    [Theory]
    [InlineData(nameof(ReportAppService.GetSalesSummaryAsync), StoreManagementPermissions.Reports.SalesSummary)]
    [InlineData(nameof(ReportAppService.GetInventoryStatusAsync), StoreManagementPermissions.Reports.InventoryStatus)]
    [InlineData(nameof(ReportAppService.GetTopSellersAsync), StoreManagementPermissions.Reports.TopSellers)]
    [InlineData(nameof(ReportAppService.GetStockMovementsAsync), StoreManagementPermissions.Reports.StockMovements)]
    [InlineData(nameof(ReportAppService.GetCategoryPerformanceAsync), StoreManagementPermissions.Reports.CategoryPerformance)]
    [InlineData(nameof(ReportAppService.GetProfitMarginAsync), StoreManagementPermissions.Reports.ProfitMargin)]
    public void Report_Method_Requires_Its_Permission(string methodName, string expectedPermission)
    {
        var method = typeof(ReportAppService).GetMethod(methodName);
        method.ShouldNotBeNull();

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();
        authorize.ShouldNotBeNull();
        authorize!.Policy.ShouldBe(expectedPermission);
    }
}
