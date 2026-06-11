using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace StoreManagement.Products;

public interface IProductVariantAppService : IApplicationService
{
    Task<PagedResultDto<ProductVariantDto>> GetListAsync(ProductVariantPagedAndSortedResultRequestDto input);

    Task<PagedResultDto<ProductVariantDto>> GetByProductAsync(
        Guid productId,
        ProductVariantPagedAndSortedResultRequestDto input
    );

    Task<PagedResultDto<ProductVariantDto>> GetDeletedListAsync(ProductVariantPagedAndSortedResultRequestDto input);

    Task<ProductVariantDto> GetAsync(Guid id);

    Task<ProductVariantOptionsDto> GetOptionsAsync(Guid productId);

    Task<ProductVariantDto> CreateAsync(CreateProductVariantDto input);

    Task<List<ProductVariantDto>> BulkCreateAsync(CreateBulkProductVariantsDto input);

    Task<List<ProductVariantDto>> GenerateAsync(GenerateProductVariantsDto input);

    Task<ProductVariantDto> UpdateAsync(Guid id, UpdateProductVariantDto input);

    Task DeleteAsync(Guid id);

    Task RestoreAsync(Guid id);
}