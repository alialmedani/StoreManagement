using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StoreManagement.Common;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace StoreManagement.Products;

public interface IProductVariantAppService : IApplicationService
{
    Task<PagedResultDto<ProductVariantDto>> GetListAsync(StoreManagementPagedAndSortedResultRequestDto input);

    Task<PagedResultDto<ProductVariantDto>> GetByProductAsync(Guid productId, StoreManagementPagedAndSortedResultRequestDto input);

    Task<PagedResultDto<ProductVariantDto>> GetDeletedListAsync(StoreManagementPagedAndSortedResultRequestDto input);

    Task<ProductVariantDto> GetAsync(Guid id);

    Task<ProductVariantDto> CreateAsync(CreateProductVariantDto input);

    Task<List<ProductVariantDto>> BulkCreateAsync(CreateBulkProductVariantsDto input);

    Task<List<ProductVariantDto>> GenerateAsync(GenerateProductVariantsDto input);

    Task<ProductVariantDto> UpdateAsync(Guid id, UpdateProductVariantDto input);

    Task DeleteAsync(Guid id);

    Task RestoreAsync(Guid id);

    Task<List<string>> GetAvailableColorsAsync(Guid productId);

    Task<List<string>> GetAvailableSizesAsync(Guid productId);

    Task<List<string>> GetAvailableColorsBySizeAsync(Guid productId, string size);

    Task<List<string>> GetAvailableSizesByColorAsync(Guid productId, string color);
}