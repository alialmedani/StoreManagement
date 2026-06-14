using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace StoreManagement.Inventory;

public interface IInventoryAppService : IApplicationService
{
    Task<PagedResultDto<StockMovementDto>> GetListAsync(
        StockMovementPagedRequestDto input
    );

    Task<StockMovementDto> GetAsync(Guid id);

    Task<InventoryVariantHistorySummaryDto> GetVariantHistorySummaryAsync(
        Guid productVariantId
    );

    Task<StockMovementDto> AdjustStockAsync(AdjustStockDto input);
}