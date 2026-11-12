using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace StoreManagement.Reports;

public interface IReportAppService : IApplicationService
{
    Task<SalesSummaryDto> GetSalesSummaryAsync(
        SalesSummaryRequestDto input
    );

    Task<InventoryStatusDto> GetInventoryStatusAsync(
        InventoryStatusRequestDto input
    );

    Task<TopSellersDto> GetTopSellersAsync(
        TopSellersRequestDto input
    );

    Task<PagedResultDto<ReportStockMovementDto>> GetStockMovementsAsync(
        ReportStockMovementRequestDto input
    );

    Task<CategoryPerformanceDto> GetCategoryPerformanceAsync(
        CategoryPerformanceRequestDto input
    );

    Task<ProfitMarginDto> GetProfitMarginAsync(
        ProfitMarginRequestDto input
    );
}
