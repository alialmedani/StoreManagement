using System;
using System.Threading.Tasks;
using StoreManagement.Common;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace StoreManagement.Products;

public interface IProductAppService : IApplicationService
{
    Task<PagedResultDto<ProductDto>> GetListAsync(StoreManagementPagedAndSortedResultRequestDto input);

    Task<PagedResultDto<ProductDto>> GetDeletedListAsync(StoreManagementPagedAndSortedResultRequestDto input);

    Task<ProductDetailsDto> GetAsync(Guid id);

    Task<ProductDto> CreateAsync(CreateProductDto input);

    Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto input);

    Task DeleteAsync(Guid id);

    Task RestoreAsync(Guid id);
}