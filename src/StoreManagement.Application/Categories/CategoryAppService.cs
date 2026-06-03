using System;
using System.Linq;
using System.Threading.Tasks;
using StoreManagement.Common;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using StoreManagement.Products;
 

namespace StoreManagement.Categories;

public class CategoryAppService : ApplicationService, ICategoryAppService
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly IDataFilter<ISoftDelete> _softDeleteFilter;

     
    public CategoryAppService(
        IRepository<Category, Guid> categoryRepository,
        IRepository<Product, Guid> productRepository,
        IDataFilter<ISoftDelete> softDeleteFilter)
    {
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
        _softDeleteFilter = softDeleteFilter;
    }
    public async Task<PagedResultDto<CategoryDto>> GetListAsync(StoreManagementPagedAndSortedResultRequestDto input)
    {
        var query = await _categoryRepository.GetQueryableAsync();

        query = ApplyFilter(query, input.Filter);
        query = ApplySorting(query, input.Sorting);

        var totalCount = await AsyncExecuter.CountAsync(query);

        var categories = await AsyncExecuter.ToListAsync(
            query
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
        );

        return new PagedResultDto<CategoryDto>(
            totalCount,
            categories.Select(MapToDto).ToList()
        );
    }

    public async Task<PagedResultDto<CategoryDto>> GetDeletedListAsync(StoreManagementPagedAndSortedResultRequestDto input)
    {
        using (_softDeleteFilter.Disable())
        {
            var query = await _categoryRepository.GetQueryableAsync();

            query = query.Where(category => category.IsDeleted);

            query = ApplyFilter(query, input.Filter);
            query = ApplySorting(query, input.Sorting);

            var totalCount = await AsyncExecuter.CountAsync(query);

            var categories = await AsyncExecuter.ToListAsync(
                query
                    .Skip(input.SkipCount)
                    .Take(input.MaxResultCount)
            );

            return new PagedResultDto<CategoryDto>(
                totalCount,
                categories.Select(MapToDto).ToList()
            );
        }
    }

    public async Task<CategoryDto> GetAsync(Guid id)
    {
        var category = await _categoryRepository.GetAsync(id);

        return MapToDto(category);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto input)
    {
        var normalizedName = NormalizeName(input.Name);

        var duplicateExists = await _categoryRepository.AnyAsync(category =>
            category.NormalizedName == normalizedName);

        if (duplicateExists)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.CategoryNameAlreadyExists);
        }

        var category = new Category(
            GuidGenerator.Create(),
            input.Name,
            input.Description,
            input.SizeType,
            input.IsActive
        );

        await _categoryRepository.InsertAsync(category, autoSave: true);

        return MapToDto(category);
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryDto input)
    {
        var category = await _categoryRepository.GetAsync(id);

        var normalizedName = NormalizeName(input.Name);

        var duplicateExists = await _categoryRepository.AnyAsync(otherCategory =>
            otherCategory.Id != id &&
            otherCategory.NormalizedName == normalizedName);

        if (duplicateExists)
        {
            throw new BusinessException(StoreManagementDomainErrorCodes.CategoryNameAlreadyExists);
        }
        if (category.SizeType != input.SizeType)
        {
            await EnsureCategoryHasNoProductsAsync(
                category.Id,
                StoreManagementDomainErrorCodes.CategorySizeTypeCannotBeChanged
            );
        }
        category.Rename(input.Name);
        category.SetDescription(input.Description);
        category.ChangeSizeType(input.SizeType);
        category.SetActive(input.IsActive);

        await _categoryRepository.UpdateAsync(category, autoSave: true);

        return MapToDto(category);
    }

    public async Task DeleteAsync(Guid id)
    {
        var category = await _categoryRepository.GetAsync(id);

        await EnsureCategoryHasNoProductsAsync(
            category.Id,
            StoreManagementDomainErrorCodes.CategoryHasProducts
        );

        await _categoryRepository.DeleteAsync(category, autoSave: true);
    }

    public async Task RestoreAsync(Guid id)
    {
        using (_softDeleteFilter.Disable())
        {
            var category = await _categoryRepository.FindAsync(id);

            if (category == null || !category.IsDeleted)
            {
                throw new EntityNotFoundException(typeof(Category), id);
            }

            var duplicateExists = await _categoryRepository.AnyAsync(otherCategory =>
                otherCategory.Id != id &&
                !otherCategory.IsDeleted &&
                otherCategory.NormalizedName == category.NormalizedName);

            if (duplicateExists)
            {
                throw new BusinessException(StoreManagementDomainErrorCodes.CategoryNameAlreadyExists);
            }

            category.Restore();

            await _categoryRepository.UpdateAsync(category, autoSave: true);
        }
    }

    private static IQueryable<Category> ApplyFilter(IQueryable<Category> query, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return query;
        }

        var normalizedFilter = filter.Trim();

        return query.Where(category =>
            category.Name.Contains(normalizedFilter) ||
            (category.Description != null && category.Description.Contains(normalizedFilter)));
    }

    private static IQueryable<Category> ApplySorting(IQueryable<Category> query, string? sorting)
    {
        if (string.IsNullOrWhiteSpace(sorting))
        {
            return query.OrderBy(category => category.CreationTime);
        }

        return sorting.Trim().ToLowerInvariant() switch
        {
            "name" or "name asc" => query.OrderBy(category => category.Name),
            "name desc" => query.OrderByDescending(category => category.Name),

            "creationtime" or "creationtime asc" => query.OrderBy(category => category.CreationTime),
            "creationtime desc" => query.OrderByDescending(category => category.CreationTime),

            "isactive" or "isactive asc" => query.OrderBy(category => category.IsActive),
            "isactive desc" => query.OrderByDescending(category => category.IsActive),

            "sizetype" or "sizetype asc" => query.OrderBy(category => category.SizeType),
            "sizetype desc" => query.OrderByDescending(category => category.SizeType),

            _ => query.OrderBy(category => category.CreationTime)
        };
    }

    private static CategoryDto MapToDto(Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            SizeType = new LookupDto
            {
                Id = (int)category.SizeType,
                Name = category.SizeType.ToString()
            },
            CreationTime = category.CreationTime,
            CreatorId = category.CreatorId,
            LastModificationTime = category.LastModificationTime,
            LastModifierId = category.LastModifierId,
            IsDeleted = category.IsDeleted,
            DeleterId = category.DeleterId,
            DeletionTime = category.DeletionTime
        };
    }

    private static string NormalizeName(string name)
    {
        return name.Trim().ToUpperInvariant();
    }
    private async Task EnsureCategoryHasNoProductsAsync(Guid categoryId, string errorCode)
    {
        using (_softDeleteFilter.Disable())
        {
            var hasProducts = await _productRepository.AnyAsync(product =>
                product.CategoryId == categoryId);

            if (hasProducts)
            {
                throw new BusinessException(errorCode);
            }
        }
    }
}