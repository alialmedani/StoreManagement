using System;
using System.Threading.Tasks;
using StoreManagement.Common;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace StoreManagement.Categories;

public interface ICategoryAppService : IApplicationService
{
    Task<PagedResultDto<CategoryDto>> GetListAsync(StoreManagementPagedAndSortedResultRequestDto input);

    Task<PagedResultDto<CategoryDto>> GetDeletedListAsync(StoreManagementPagedAndSortedResultRequestDto input);

    Task<CategoryDto> GetAsync(Guid id);

    Task<CategoryDto> CreateAsync(CreateCategoryDto input);

    Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryDto input);

    Task DeleteAsync(Guid id);

    Task RestoreAsync(Guid id);
}