using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace StoreManagement.Orders;

public interface IOrderAppService : IApplicationService
{
    Task<PagedResultDto<OrderDto>> GetListAsync(
        OrderPagedRequestDto input
    );

    Task<OrderDetailsDto> GetAsync(Guid id);

    Task<OrderDetailsDto> CreateAsync(
        CreateOrderDto input
    );

    Task<OrderDetailsDto> UpdateAsync(
        Guid id,
        UpdateOrderDto input
    );

    Task<OrderDetailsDto> AddItemAsync(
        Guid id,
        AddOrderItemDto input
    );

    Task<OrderDetailsDto> UpdateItemAsync(
        Guid id,
        Guid itemId,
        UpdateOrderItemDto input
    );

    Task<OrderDetailsDto> RemoveItemAsync(
        Guid id,
        Guid itemId
    );

    Task<OrderDetailsDto> ConfirmAsync(Guid id);

    Task<OrderDetailsDto> RecordPaymentAsync(
        Guid id,
        RecordOrderPaymentDto input
    );

    Task<OrderDetailsDto> CancelAsync(
        Guid id,
        CancelOrderDto input
    );

    Task DeleteAsync(Guid id);
}