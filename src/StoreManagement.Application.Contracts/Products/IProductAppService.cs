using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace StoreManagement.Products;

public interface IProductAppService : IApplicationService
{
    Task<PagedResultDto<ProductDto>> GetListAsync(ProductPagedAndSortedResultRequestDto input);

    Task<PagedResultDto<ProductDto>> GetDeletedListAsync(ProductPagedAndSortedResultRequestDto input);

    Task<ProductDashboardSummaryDto> GetDashboardSummaryAsync();

    Task<ProductDetailsDto> GetAsync(Guid id);

    Task<ProductDto> CreateAsync(CreateProductDto input);

    Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto input);

    Task DeleteAsync(Guid id);

    Task RestoreAsync(Guid id);
}