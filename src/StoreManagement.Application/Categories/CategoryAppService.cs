using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using StoreManagement.Common;
using StoreManagement.Permissions;
using StoreManagement.Products;
using StoreManagement.Settings;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Settings;

namespace StoreManagement.Categories;

public class CategoryAppService : ApplicationService, ICategoryAppService
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly IDataFilter<ISoftDelete> _softDeleteFilter;
    private readonly ISettingProvider _settingProvider;

    public CategoryAppService(
        IRepository<Category, Guid> categoryRepository,
        IRepository<Product, Guid> productRepository,
        IDataFilter<ISoftDelete> softDeleteFilter,
        ISettingProvider settingProvider)
    {
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
        _softDeleteFilter = softDeleteFilter;
        _settingProvider = settingProvider;
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

    public async Task<PagedResultDto<CategoryStockSummaryDto>> GetStockSummaryAsync(CategoryStockSummaryRequestDto input)
    {
        var lowStockThreshold = await GetLowStockThresholdAsync();

        var categoryQuery = await _categoryRepository.GetQueryableAsync();

        categoryQuery = ApplyFilter(categoryQuery, input.Filter);

        var categories = await AsyncExecuter.ToListAsync(categoryQuery);

        var productQuery = await _productRepository.GetQueryableAsync();

        var productStocks = await AsyncExecuter.ToListAsync(
            productQuery
                .Select(product => new CategoryProductStock(
                    product.CategoryId,
                    product.IsActive,
                    product.Variants
                        .Where(variant => !variant.IsDeleted)
                        .Sum(variant => variant.StockQuantity),
                    product.Variants
                        .Count(variant => !variant.IsDeleted),
                    product.Variants
                        .Count(variant => !variant.IsDeleted && variant.IsActive),
                    product.Variants
                        .Count(variant => !variant.IsDeleted && !variant.IsActive),
                    product.Variants
                        .Count(variant => !variant.IsDeleted && variant.StockQuantity <= 0),
                    product.Variants
                        .Count(variant =>
                            !variant.IsDeleted &&
                            variant.StockQuantity > 0 &&
                            variant.StockQuantity <= lowStockThreshold),
                    product.Variants
                        .Count(variant =>
                            !variant.IsDeleted &&
                            variant.StockQuantity > lowStockThreshold)
                ))
        );

        var productStocksByCategory = productStocks
            .GroupBy(product => product.CategoryId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var summaries = categories
            .Select(category =>
            {
                productStocksByCategory.TryGetValue(category.Id, out var categoryProducts);

                categoryProducts ??= new List<CategoryProductStock>();

                var productStatuses = categoryProducts
                    .Select(product => ProductAvailabilityCalculator.CalculateStatus(
                        product.TotalStockQuantity,
                        lowStockThreshold
                    ))
                    .ToList();

                var totalStockQuantity = categoryProducts.Sum(product => product.TotalStockQuantity);

                return new CategoryStockSummaryDto
                {
                    CategoryId = category.Id,
                    CategoryName = category.Name,
                    IsActive = category.IsActive,

                    ProductCount = categoryProducts.Count,
                    ActiveProducts = categoryProducts.Count(product => product.IsActive),
                    InactiveProducts = categoryProducts.Count(product => !product.IsActive),

                    VariantCount = categoryProducts.Sum(product => product.VariantCount),
                    ActiveVariants = categoryProducts.Sum(product => product.ActiveVariantCount),
                    InactiveVariants = categoryProducts.Sum(product => product.InactiveVariantCount),

                    TotalStockQuantity = totalStockQuantity,
                    LowStockThreshold = lowStockThreshold,

                    AvailabilityStatus = ProductAvailabilityCalculator.CreateStatus(
                        totalStockQuantity,
                        lowStockThreshold
                    ),

                    OutOfStockProducts = productStatuses.Count(status =>
                        status == ProductAvailabilityStatus.OutOfStock),

                    LowStockProducts = productStatuses.Count(status =>
                        status == ProductAvailabilityStatus.LowStock),

                    InStockProducts = productStatuses.Count(status =>
                        status == ProductAvailabilityStatus.InStock),

                    OutOfStockVariants = categoryProducts.Sum(product => product.OutOfStockVariants),
                    LowStockVariants = categoryProducts.Sum(product => product.LowStockVariants),
                    InStockVariants = categoryProducts.Sum(product => product.InStockVariants)
                };
            })
            .ToList();

        summaries = ApplyAvailabilityStatusFilter(
            summaries,
            input.AvailabilityStatus,
            lowStockThreshold
        );

        summaries = ApplyStockSummarySorting(summaries, input.Sorting);

        var totalCount = summaries.Count;

        var items = summaries
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        return new PagedResultDto<CategoryStockSummaryDto>(totalCount, items);
    }

    public async Task<CategoryDto> GetAsync(Guid id)
    {
        var category = await _categoryRepository.GetAsync(id);

        return MapToDto(category);
    }

    [Authorize(StoreManagementPermissions.Categories.Create)]
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

    [Authorize(StoreManagementPermissions.Categories.Edit)]
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

    [Authorize(StoreManagementPermissions.Categories.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var category = await _categoryRepository.GetAsync(id);

        await EnsureCategoryHasNoProductsAsync(
            category.Id,
            StoreManagementDomainErrorCodes.CategoryHasProducts
        );

        await _categoryRepository.DeleteAsync(category, autoSave: true);
    }

    [Authorize(StoreManagementPermissions.Categories.Restore)]
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

    private static List<CategoryStockSummaryDto> ApplyAvailabilityStatusFilter(
        List<CategoryStockSummaryDto> summaries,
        ProductAvailabilityStatus? availabilityStatus,
        int lowStockThreshold)
    {
        if (!availabilityStatus.HasValue)
        {
            return summaries;
        }

        return availabilityStatus.Value switch
        {
            ProductAvailabilityStatus.OutOfStock => summaries
                .Where(summary => summary.TotalStockQuantity <= 0)
                .ToList(),

            ProductAvailabilityStatus.LowStock => summaries
                .Where(summary =>
                    summary.TotalStockQuantity > 0 &&
                    summary.TotalStockQuantity <= lowStockThreshold)
                .ToList(),

            ProductAvailabilityStatus.InStock => summaries
                .Where(summary => summary.TotalStockQuantity > lowStockThreshold)
                .ToList(),

            _ => summaries
        };
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

    private static List<CategoryStockSummaryDto> ApplyStockSummarySorting(
        List<CategoryStockSummaryDto> summaries,
        string? sorting)
    {
        if (string.IsNullOrWhiteSpace(sorting))
        {
            return summaries
                .OrderBy(summary => summary.CategoryName)
                .ToList();
        }

        return sorting.Trim().ToLowerInvariant() switch
        {
            "categoryname" or "categoryname asc" or "name" or "name asc" => summaries
                .OrderBy(summary => summary.CategoryName)
                .ToList(),

            "categoryname desc" or "name desc" => summaries
                .OrderByDescending(summary => summary.CategoryName)
                .ToList(),

            "totalstockquantity" or "totalstockquantity asc" => summaries
                .OrderBy(summary => summary.TotalStockQuantity)
                .ToList(),

            "totalstockquantity desc" => summaries
                .OrderByDescending(summary => summary.TotalStockQuantity)
                .ToList(),

            "productcount" or "productcount asc" => summaries
                .OrderBy(summary => summary.ProductCount)
                .ToList(),

            "productcount desc" => summaries
                .OrderByDescending(summary => summary.ProductCount)
                .ToList(),

            "variantcount" or "variantcount asc" => summaries
                .OrderBy(summary => summary.VariantCount)
                .ToList(),

            "variantcount desc" => summaries
                .OrderByDescending(summary => summary.VariantCount)
                .ToList(),

            _ => summaries
                .OrderBy(summary => summary.CategoryName)
                .ToList()
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

    private async Task<int> GetLowStockThresholdAsync()
    {
        var value = await _settingProvider.GetOrNullAsync(StoreManagementSettings.LowStockThreshold);

        return ProductAvailabilityCalculator.NormalizeLowStockThreshold(value);
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

    private sealed record CategoryProductStock(
        Guid CategoryId,
        bool IsActive,
        int TotalStockQuantity,
        int VariantCount,
        int ActiveVariantCount,
        int InactiveVariantCount,
        int OutOfStockVariants,
        int LowStockVariants,
        int InStockVariants
    );
}